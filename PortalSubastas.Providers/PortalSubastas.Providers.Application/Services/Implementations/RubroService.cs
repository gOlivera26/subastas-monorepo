namespace PortalSubastas.Providers.Application.Services.Implementations;

public class RubroService : BaseService, IRubroService
{
    private readonly new ProvidersContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private const int MaxDescripcionLength = 255;

    public RubroService(
        ProvidersContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IPublishEndpoint publishEndpoint)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OperationResponse<RubroListResponseDto>> GetRubrosAsync(int page, int pageSize, string? searchTerm, string? sortBy = null, string? sortDirection = null)
    {
        var query = _context.TRubros
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = $"%{searchTerm.Trim()}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.Codigo, term) ||
                EF.Functions.ILike(r.Descripcion, term));
        }

        query = (sortBy, sortDirection?.ToLower()) switch
        {
            ("codigo", "desc") => query.OrderByDescending(r => r.Codigo),
            ("codigo", _) => query.OrderBy(r => r.Codigo),
            ("descripcion", "desc") => query.OrderByDescending(r => r.Descripcion),
            ("descripcion", _) => query.OrderBy(r => r.Descripcion),
            _ => query.OrderBy(r => r.Codigo)
        };

        query = query.Include(r => r.IdRubroPadreNavigation);

        var result = await GetPagedDataAsync<TRubro, RubroListDto>(page, pageSize, query);
        var (data, total) = result.Data;

        return Ok(new RubroListResponseDto { Data = data, Total = total });
    }

    public async Task<OperationResponse<RubroListDto>> CreateRubroAsync(CreateRubroDto dto)
    {
        var codigoExiste = await _context.TRubros.AnyAsync(r => r.Codigo == dto.Codigo);
        if (codigoExiste)
            return BadRequest<RubroListDto>("Ya existe un rubro con ese código.");

        if (dto.IdRubroPadre.HasValue)
        {
            var padre = await _context.TRubros.FindAsync(dto.IdRubroPadre.Value);
            if (padre == null)
                return BadRequest<RubroListDto>("El rubro padre no existe.");
        }

        var rubro = _mapper.Map<TRubro>(dto);
        PrepareAuditableEntity(rubro, isNew: true);
        _context.TRubros.Add(rubro);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "RUBRO_CREADO", "RUBROS",
            new { RubroId = rubro.Id, Codigo = rubro.Codigo, Descripcion = rubro.Descripcion });

        return Ok(_mapper.Map<RubroListDto>(rubro));
    }

    public async Task<OperationResponse<RubroListDto>> UpdateRubroAsync(UpdateRubroDto dto)
    {
        var rubro = await _context.TRubros.FindAsync(dto.Id);
        if (rubro == null)
            return NotFound<RubroListDto>();

        var codigoExiste = await _context.TRubros.AnyAsync(r => r.Codigo == dto.Codigo && r.Id != dto.Id);
        if (codigoExiste)
            return BadRequest<RubroListDto>("Ya existe otro rubro con ese código.");

        if (dto.IdRubroPadre.HasValue && dto.IdRubroPadre.Value == dto.Id)
            return BadRequest<RubroListDto>("Un rubro no puede ser su propio padre.");

        _mapper.Map(dto, rubro);
        PrepareAuditableEntity(rubro, isNew: false);
        _context.TRubros.Update(rubro);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "RUBRO_ACTUALIZADO", "RUBROS",
            new { RubroId = rubro.Id, Codigo = rubro.Codigo });

        return Ok(_mapper.Map<RubroListDto>(rubro));
    }

    public async Task<OperationResponse<bool>> DeleteRubroAsync(int id)
    {
        var rubro = await _context.TRubros.FindAsync(id);
        if (rubro == null)
            return NotFound<bool>();

        var tieneHijos = await _context.TRubros.AnyAsync(r => r.IdRubroPadre == id && r.FecBaja == null);
        if (tieneHijos)
            return BadRequest<bool>("No se puede eliminar un rubro que tiene hijos. Elimine primero los rubros hijos.");

        var tieneProveedores = await _context.TProveedoresRubros.AnyAsync(pr => pr.IdRubro == id && pr.FecBaja == null);
        if (tieneProveedores)
            return BadRequest<bool>("No se puede eliminar un rubro que tiene proveedores vinculados.");

        PrepareAuditableEntity(rubro, isNew: false, isDeleted: true);
        _context.TRubros.Update(rubro);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "RUBRO_ELIMINADO", "RUBROS",
            new { RubroId = id, Codigo = rubro.Codigo });

        return Ok(true);
    }

    public async Task<OperationResponse<List<RubroTreeDto>>> GetRubroTreeAsync()
    {
        var rubros = await _context.TRubros
            .AsNoTracking()
            .Where(r => r.FecBaja == null)
            .OrderBy(r => r.Codigo)
            .ToListAsync();

        return Ok(BuildRubroTree(rubros, parentId: null));
    }

    public async Task<OperationResponse<List<RubroTreeDto>>> GetRubroChildrenAsync(int parentId)
    {
        var rubros = await _context.TRubros
            .AsNoTracking()
            .Where(r => r.FecBaja == null)
            .OrderBy(r => r.Codigo)
            .ToListAsync();

        return Ok(BuildRubroTree(rubros, parentId));
    }

    public async Task<OperationResponse<List<RubroSearchResultDto>>> SearchRubrosAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest<List<RubroSearchResultDto>>("La busqueda requiere al menos un caracter.");

        var searchTerm = $"%{query.Trim()}%";
        var results = await _context.TRubros
            .AsNoTracking()
            .Where(r => r.FecBaja == null &&
                        (EF.Functions.ILike(r.Codigo, searchTerm) || EF.Functions.ILike(r.Descripcion, searchTerm)))
            .OrderBy(r => r.Codigo)
            .Select(r => new RubroSearchResultDto
            {
                Id = r.Id,
                Codigo = r.Codigo,
                Descripcion = r.Descripcion,
                HasChildren = _context.TRubros.Any(c => c.IdRubroPadre == r.Id && c.FecBaja == null),
                Level = 0
            })
            .ToListAsync();

        return Ok(results);
    }

    public async Task<OperationResponse<RubroBulkUploadResultDto>> BulkUploadRubrosAsync(Stream fileStream)
    {
        var result = new RubroBulkUploadResultDto();
        var rows = new List<LegacyRubroRow>();

        using (var reader = new StreamReader(fileStream, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
        {
            var header = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(header))
                return BadRequest<RubroBulkUploadResultDto>("El archivo no contiene encabezados.");

            var headers = ParseCsvLine(header).Select(NormalizeHeader).ToList();
            var indexes = headers
                .Select((name, index) => new { name, index })
                .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

            var required = new[] { "ID_RUBRO_PROV", "CODIGO", "NOMBRE" };
            var missing = required.Where(column => !indexes.ContainsKey(column)).ToList();
            if (missing.Count > 0)
                return BadRequest<RubroBulkUploadResultDto>($"Faltan columnas obligatorias: {string.Join(", ", missing)}.");

            var lineNumber = 1;
            while (!reader.EndOfStream)
            {
                lineNumber++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var values = ParseCsvLine(line);
                string GetValue(string column)
                {
                    if (!indexes.TryGetValue(column, out var index) || index >= values.Count)
                        return string.Empty;
                    return values[index].Trim();
                }

                var legacyIdText = GetValue("ID_RUBRO_PROV");
                var codigo = GetValue("CODIGO");
                var nombre = GetValue("NOMBRE");

                if (!int.TryParse(legacyIdText, out var legacyId))
                {
                    result.Errores.Add($"Linea {lineNumber}: ID_RUBRO_PROV invalido.");
                    result.Omitidos++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
                {
                    result.Errores.Add($"Linea {lineNumber}: codigo o nombre vacio.");
                    result.Omitidos++;
                    continue;
                }

                int? parentLegacyId = null;
                var parentText = GetValue("ID_RUBRO_PROV_REL");
                if (!string.IsNullOrWhiteSpace(parentText))
                {
                    if (int.TryParse(parentText, out var parsedParent))
                        parentLegacyId = parsedParent;
                    else
                        result.Errores.Add($"Linea {lineNumber}: ID_RUBRO_PROV_REL invalido; se importara sin padre.");
                }

                rows.Add(new LegacyRubroRow
                {
                    LegacyId = legacyId,
                    ParentLegacyId = parentLegacyId,
                    Codigo = codigo,
                    Descripcion = Truncate(nombre, MaxDescripcionLength),
                    Imputable = ParseLegacyBoolean(GetValue("IMPUTABLE")),
                    Activo = ParseLegacyBoolean(GetValue("ACTIVO"), defaultValue: true),
                    LineNumber = lineNumber
                });
            }
        }

        var duplicatedLegacyIds = rows
            .GroupBy(r => r.LegacyId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        foreach (var duplicate in duplicatedLegacyIds)
            result.Errores.Add($"ID_RUBRO_PROV duplicado en CSV: {duplicate}. Se usara la primera ocurrencia.");

        rows = rows
            .GroupBy(r => r.LegacyId)
            .Select(g => g.First())
            .ToList();

        result.Procesados = rows.Count;
        if (rows.Count == 0)
            return BadRequest<RubroBulkUploadResultDto>("No se encontraron rubros validos para importar.", result);

        var codes = rows.Select(r => r.Codigo).Distinct().ToList();
        var existing = await _context.TRubros
            .Where(r => codes.Contains(r.Codigo))
            .ToDictionaryAsync(r => r.Codigo);

        var legacyToEntity = new Dictionary<int, TRubro>();

        foreach (var row in rows)
        {
            if (existing.TryGetValue(row.Codigo, out var rubro))
            {
                rubro.Descripcion = row.Descripcion;
                rubro.Imputable = row.Imputable;
                if (row.Activo)
                {
                    rubro.FecBaja = null;
                    rubro.UsrBaja = null;
                }
                else
                {
                    PrepareAuditableEntity(rubro, isNew: false, isDeleted: true);
                }
                PrepareAuditableEntity(rubro, isNew: false);
                result.Actualizados++;
            }
            else
            {
                rubro = new TRubro
                {
                    Codigo = row.Codigo,
                    Descripcion = row.Descripcion,
                    Imputable = row.Imputable
                };

                PrepareAuditableEntity(rubro, isNew: true);
                if (!row.Activo)
                    PrepareAuditableEntity(rubro, isNew: false, isDeleted: true);

                _context.TRubros.Add(rubro);
                existing[row.Codigo] = rubro;
                result.Creados++;
            }

            legacyToEntity[row.LegacyId] = rubro;
        }

        await _context.SaveChangesAsync();

        foreach (var row in rows)
        {
            var rubro = legacyToEntity[row.LegacyId];
            int? parentId = null;

            if (row.ParentLegacyId.HasValue)
            {
                if (legacyToEntity.TryGetValue(row.ParentLegacyId.Value, out var parent))
                {
                    if (parent.Id == rubro.Id)
                    {
                        result.Errores.Add($"Linea {row.LineNumber}: el rubro no puede ser padre de si mismo.");
                    }
                    else
                    {
                        parentId = parent.Id;
                    }
                }
                else
                {
                    result.Errores.Add($"Linea {row.LineNumber}: padre legacy {row.ParentLegacyId.Value} no encontrado en el archivo.");
                }
            }

            if (rubro.IdRubroPadre != parentId)
            {
                rubro.IdRubroPadre = parentId;
                PrepareAuditableEntity(rubro, isNew: false);
                result.RelacionesActualizadas++;
            }
        }

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "RUBROS_IMPORTADOS", "RUBROS",
            new { result.Procesados, result.Creados, result.Actualizados, result.RelacionesActualizadas, result.Omitidos });

        return OkMasive(result, result.Procesados);
    }

    private static List<RubroTreeDto> BuildRubroTree(List<TRubro> rubros, int? parentId)
    {
        var byParent = rubros
            .GroupBy(r => r.IdRubroPadre)
            .ToLookup(g => g.Key, g => g.OrderBy(r => r.Codigo).ToList());

        List<RubroTreeDto> Build(int? currentParentId, int depth)
        {
            var children = byParent[currentParentId].FirstOrDefault();
            if (children is null)
                return new List<RubroTreeDto>();

            return children
                .Select(r =>
                {
                    var hasChildren = byParent[r.Id].Any();
                    return new RubroTreeDto
                    {
                        Id = r.Id,
                        Codigo = r.Codigo,
                        Descripcion = r.Descripcion,
                        IdRubroPadre = r.IdRubroPadre,
                        Imputable = r.Imputable,
                        HasChildren = hasChildren,
                        Children = depth < 2 && hasChildren
                            ? Build(r.Id, depth + 1)
                            : new List<RubroTreeDto>()
                    };
                })
                .ToList();
        }

        return Build(parentId, 0);
    }

    private static string NormalizeHeader(string value)
        => value.Trim().Trim('"').ToUpperInvariant();

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private static bool ParseLegacyBoolean(string value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value.Trim().ToUpperInvariant() switch
        {
            "S" or "SI" or "SÍ" or "TRUE" or "1" or "Y" => true,
            "N" or "NO" or "FALSE" or "0" => false,
            _ => defaultValue
        };
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ';' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    private sealed class LegacyRubroRow
    {
        public int LegacyId { get; init; }
        public int? ParentLegacyId { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Descripcion { get; init; } = string.Empty;
        public bool Imputable { get; init; }
        public bool Activo { get; init; }
        public int LineNumber { get; init; }
    }
}

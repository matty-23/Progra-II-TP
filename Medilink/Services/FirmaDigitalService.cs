using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

public class FirmaDigitalService
{
    private readonly string _secretKey;
    private readonly JsonSerializerOptions _firmaOptions;

    public FirmaDigitalService(IConfiguration config)
    {
        _secretKey = config["Hospital:SecretKey"] ?? "HOSPITAL_SECRET_KEY";
        
        // ✅ Opciones CRÍTICAS para que la firma sea consistente
        _firmaOptions = new JsonSerializerOptions
        {
            // ❌ NO usar ReferenceHandler.Preserve (añade $id, $ref que rompen la firma)
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            
            // Ignorar propiedades null
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            
            // CamelCase consistente
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            
            // Sin indentación (compacto)
            WriteIndented = false,
            
            // Convertir enums a strings
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public JsonSerializerOptions GetFirmaOptions()
    {
        return _firmaOptions;
    }

    public string GenerarFirma(object data)
    {
        // Serializar con las opciones configuradas
        var json = JsonSerializer.Serialize(data, _firmaOptions);
        
        // Calcular hash HMAC-SHA256
        using var sha = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
        
        // Devolver en Base64
        return Convert.ToBase64String(hash);
    }
}
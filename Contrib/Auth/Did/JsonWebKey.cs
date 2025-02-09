using System; // ReadOnlySpan
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Basis.Contrib.Auth.DecentralizedIds
{
	public class JsonWebKey
	{
		[JsonPropertyName("kty")]
		public string? Kty { get; set; }

		[JsonPropertyName("kid")]
		public string? Kid { get; set; }

		[JsonPropertyName("alg")]
		public string? Alg { get; set; }

		[JsonPropertyName("use")]
		public string? Use { get; set; }

		// Ed25519 parameters
		[JsonPropertyName("x")]
		public string? X { get; set; }

		[JsonPropertyName("d")]
		public string? D { get; set; }

		[JsonPropertyName("crv")]
		public string? Crv { get; set; }

		// Symmetric key parameter
		[JsonPropertyName("k")]
		public string? K { get; set; }

		// Helper method to exclude null values during serialization
		public static JsonSerializerOptions SerializerOptions =>
			new()
			{
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				WriteIndented = true,
			};

		public string Serialize()
		{
			return JsonSerializer.Serialize(this, SerializerOptions);
		}

		public static JsonWebKey? Deserialize(string json)
		{
			return JsonSerializer.Deserialize<JsonWebKey>(json, SerializerOptions);
		}

		public static JsonWebKey? Deserialize(ReadOnlySpan<byte> json)
		{
			return JsonSerializer.Deserialize<JsonWebKey>(json, SerializerOptions);
		}
	}
}

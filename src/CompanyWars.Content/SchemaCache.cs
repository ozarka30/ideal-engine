using System.Security.Cryptography;
using System.Text;
using Json.Schema;

namespace CompanyWars.Content;

/// <summary>
/// JsonSchema.Net registers a schema's <c>$id</c> globally on parse and refuses to register it twice, so a schema
/// file is parsed once per distinct content and shared. Two files with the same <c>$id</c> but different text
/// (a scratch copy that edits the schema) are an error rather than a silent overwrite.
/// </summary>
public static class SchemaCache
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, (string Hash, JsonSchema Schema)> ById = new(StringComparer.Ordinal);

    public static JsonSchema Load(string path, string expectedId)
    {
        string text = File.ReadAllText(path);
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        lock (Gate)
        {
            if (ById.TryGetValue(expectedId, out (string Hash, JsonSchema Schema) cached))
            {
                if (cached.Hash != hash) throw new ContentException($"{path}: a different schema with $id {expectedId} is already loaded in this process");
                return cached.Schema;
            }
            JsonSchema schema = JsonSchema.FromText(text);
            ById[expectedId] = (hash, schema);
            return schema;
        }
    }
}

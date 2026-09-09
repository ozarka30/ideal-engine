using CompanyWars.Content;

namespace CompanyWars.Tools;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("usage: CompanyWars.Tools <validate-content|validate-manifest|fixtures|screenshot-compare> [args]");
            return 2;
        }
        try
        {
            switch (args[0])
            {
                case "validate-content":
                    return ValidateContent(Arg(args, 1) ?? RepoRoot.Find());
                case "validate-manifest":
                    return ValidateManifest(Arg(args, 1) ?? RepoRoot.Find());
                case "fixtures":
                    return Fixtures.Run(args.Skip(1).ToArray());
                case "screenshot-compare":
                    return Screenshots.Compare(Arg(args, 1) ?? RepoRoot.Find());
                default:
                    Console.Error.WriteLine($"unknown command {args[0]}");
                    return 2;
            }
        }
        catch (ContentException ex)
        {
            Console.Error.WriteLine("content is invalid:");
            foreach (string e in ex.Errors.Count > 0 ? ex.Errors : new[] { ex.Message }) Console.Error.WriteLine("  " + e);
            return 1;
        }
    }

    private static string? Arg(string[] args, int i) => args.Length > i ? args[i] : null;

    private static int ValidateContent(string root)
    {
        ContentDb db = ContentLoader.Load(root);
        Console.WriteLine($"content {db.ContentVersion}: {db.Employees.Length} employees, {db.Rooms.Length} rooms, {db.Furniture.Length} furniture, {db.Recipes.Length} recipes, {db.Riders.Length} riders, {db.Modifiers.Length} modifiers, {db.Founders.Length} founders, {db.Templates.Templates.Length} templates, {db.ScriptedRivals.Length} scripted rivals: valid");
        return 0;
    }

    private static int ValidateManifest(string root)
    {
        ContentDb db = ContentLoader.Load(root);
        CompanyWars.Manifest.ManifestReport report = CompanyWars.Manifest.ManifestValidator.Validate(root, db);
        string coveragePath = Path.Combine(root, "art_coverage.json");
        File.WriteAllText(coveragePath, report.CoverageJson());
        if (report.Errors.Count > 0)
        {
            Console.Error.WriteLine("manifest is invalid:");
            foreach (string e in report.Errors) Console.Error.WriteLine("  " + e);
            return 1;
        }
        Console.WriteLine($"manifest: {report.Coverage.Total} entries, {report.Coverage.Gated} gated, {report.Coverage.WithArt} with art, {report.Coverage.VerifyPending} verify pending: valid; wrote {coveragePath}");
        return 0;
    }
}

public static class RepoRoot
{
    /// <summary>Walks up from the current directory to the directory holding CompanyWars.sln.</summary>
    public static string Find(string? start = null)
    {
        string dir = start ?? Directory.GetCurrentDirectory();
        while (true)
        {
            if (File.Exists(Path.Combine(dir, "CompanyWars.sln"))) return dir;
            string? parent = Path.GetDirectoryName(dir);
            if (parent == null || parent == dir) throw new InvalidOperationException("CompanyWars.sln not found above " + (start ?? Directory.GetCurrentDirectory()));
            dir = parent;
        }
    }
}

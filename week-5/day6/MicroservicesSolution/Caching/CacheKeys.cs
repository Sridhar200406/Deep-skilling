namespace Caching
{
    /// <summary>
    /// Centralised cache key factories.
    /// All cache keys for Employee and Department data live here
    /// so services never hard-code strings.
    /// </summary>
    public static class CacheKeys
    {
        // ── Employee ─────────────────────────────────────────────────────────
        public static string Employee(int id)         => $"employee:{id}";
        public static string EmployeeList(string tag) => $"employees:list:{tag}";
        public static string EmployeeListAll()        => "employees:list:all";

        // ── Department ───────────────────────────────────────────────────────
        public static string Department(int id)    => $"department:{id}";
        public static string DepartmentListAll()   => "departments:list:all";
    }
}

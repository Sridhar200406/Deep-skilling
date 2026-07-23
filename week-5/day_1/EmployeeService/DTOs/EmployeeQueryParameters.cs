namespace EmployeeService.DTOs
{
    /// <summary>Query parameters for filtering, sorting, and paginating employee lists.</summary>
    public class EmployeeQueryParameters
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public string? SearchTerm { get; set; }
        public int? DepartmentId { get; set; }
        public string SortBy { get; set; } = "EmployeeId";
        public bool IsAscending { get; set; } = true;
    }
}

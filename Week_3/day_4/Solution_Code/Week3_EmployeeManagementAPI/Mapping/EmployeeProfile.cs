using AutoMapper;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Mapping
{
    /// <summary>
    /// AutoMapper profile — defines all mappings between Employee entity and DTOs.
    /// Registered once in Program.cs; AutoMapper resolves it automatically.
    /// 
    /// Mappings configured here:
    ///   Employee        → EmployeeReadDto   (for GET responses)
    ///   EmployeeCreateDto → Employee        (for POST requests)
    ///   EmployeeUpdateDto → Employee        (for PUT requests)
    /// </summary>
    public class EmployeeProfile : Profile
    {
        public EmployeeProfile()
        {
            // ---------------------------------------------------------------
            // Employee → EmployeeReadDto
            // AutoMapper matches same-named properties automatically.
            // Custom mappings needed:
            //   FullName     = FirstName + " " + LastName
            //   DepartmentName = Department.DepartmentName (null-safe)
            // ---------------------------------------------------------------
            CreateMap<Employee, EmployeeReadDto>()
                .ForMember(
                    dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(
                    dest => dest.DepartmentName,
                    opt => opt.MapFrom(src =>
                        src.Department != null ? src.Department.DepartmentName : string.Empty));

            // ---------------------------------------------------------------
            // EmployeeCreateDto → Employee
            // EmployeeId is NOT mapped — DB assigns it.
            // Department navigation is NOT mapped — only DepartmentId is set.
            // ---------------------------------------------------------------
            CreateMap<EmployeeCreateDto, Employee>();

            // ---------------------------------------------------------------
            // EmployeeUpdateDto → Employee
            // EmployeeId IS mapped so the service can use it for the update.
            // ---------------------------------------------------------------
            CreateMap<EmployeeUpdateDto, Employee>();
        }
    }
}

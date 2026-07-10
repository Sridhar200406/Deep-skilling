using AutoMapper;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Mapping
{
    public class EmployeeProfile : Profile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeReadDto>()
                .ForMember(d => d.FullName,
                    opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ForMember(d => d.DepartmentName,
                    opt => opt.MapFrom(s =>
                        s.Department != null ? s.Department.DepartmentName : string.Empty));

            CreateMap<EmployeeCreateDto, Employee>();
            CreateMap<EmployeeUpdateDto, Employee>();
        }
    }
}

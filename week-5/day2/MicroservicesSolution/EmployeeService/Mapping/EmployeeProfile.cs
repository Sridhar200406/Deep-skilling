using AutoMapper;
using EmployeeService.DTOs;
using EmployeeService.Models;

namespace EmployeeService.Mapping
{
    public class EmployeeProfile : Profile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeReadDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ForMember(d => d.DepartmentName, o => o.Ignore()); // enriched at service layer

            CreateMap<EmployeeCreateDto, Employee>();
            CreateMap<EmployeeUpdateDto, Employee>();
        }
    }
}

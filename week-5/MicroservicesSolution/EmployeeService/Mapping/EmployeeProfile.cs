using AutoMapper;
using EmployeeService.DTOs;
using EmployeeService.Models;

namespace EmployeeService.Mapping
{
    /// <summary>AutoMapper profile for Employee entity ↔ DTOs.</summary>
    public class EmployeeProfile : Profile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeReadDto>()
                .ForMember(d => d.FullName,
                    opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));

            CreateMap<EmployeeCreateDto, Employee>();
            CreateMap<EmployeeUpdateDto, Employee>();
        }
    }
}

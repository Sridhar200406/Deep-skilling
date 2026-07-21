using AutoMapper;
using DepartmentService.DTOs;
using DepartmentService.Models;

namespace DepartmentService.Mapping
{
    public class DepartmentProfile : Profile
    {
        public DepartmentProfile()
        {
            CreateMap<Department, DepartmentReadDto>();
            CreateMap<DepartmentCreateDto, Department>();
        }
    }
}

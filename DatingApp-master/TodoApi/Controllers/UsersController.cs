using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Data;
using TodoApi.Dtos;
using System.Collections.Generic;

namespace TodoApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;

        public UsersController(IDatingRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(){
            
            var users = await _repo.GetUsers();

            var usersToReturn = _mapper.Map<IEnumerable<UserFotListDto>>(users);

            return Ok(usersToReturn);
            
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id){

            var user = await _repo.GetUser(id);

            var userToReturn = _mapper.Map<UserForDeatailDto>(user);

            return Ok(userToReturn);
        }

        


    }
}
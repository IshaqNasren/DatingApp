using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Helpers;
using TodoApi.Model;

namespace TodoApi.Controllers
{
    [Authorize]
    [Route("Users/{userId}/Photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySetting> _cloudinarySettings;
        private readonly IDatingRepository _repo;        
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repo, IMapper mapper, IOptions<CloudinarySetting> cloudinarySettings)
        {
            _repo = repo;
            _mapper = mapper;
            _cloudinarySettings = cloudinarySettings;

            Account acc = new Account(
                _cloudinarySettings.Value.CloudName,
                _cloudinarySettings.Value.ApiKey,
                _cloudinarySettings.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id){

            var photoFromRepo = await _repo.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId , [FromForm]PhotoForCreationDto photoForCreationDto){
            
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var userFromRepo = await _repo.GetUser(userId); 

            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };

                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }   

            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if(!userFromRepo.Photos.Any(u => u.IsMain))
                    photo.IsMain = true;

            userFromRepo.Photos.Add(photo);            

            if(await _repo.SaveAll())
                {
                    var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                    return CreatedAtRoute("GetPhoto" , new {Id = photo.Id}, photoToReturn);
                }
               
           return BadRequest("Could not add the photo");     

        }

        [HttpGet("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id){
            
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var user = await _repo.GetUser(userId);

            if(!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

             var photoFromRepo = await _repo.GetPhoto(id); 

            if(photoFromRepo.IsMain)
                return BadRequest("This is already the main photo!");

            var currentMainPhoto = await _repo.GetMainPhotoForUser(userId);

            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true;

            if(await _repo.SaveAll())
                return NoContent();

            return BadRequest("Couldn't set photo to main");

             
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id){

            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var user = await _repo.GetUser(userId);

            if(!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

             var photoFromRepo = await _repo.GetPhoto(id); 

            if(photoFromRepo.IsMain)
                return BadRequest("You can't remove the main photo");

            if(photoFromRepo.PublicId != null)
            {    
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);    
                var result = _cloudinary.Destroy(deleteParams);
            

            if(result.Result == "ok")            
                _repo.Delete(photoFromRepo);
            }  

            if(photoFromRepo.PublicId != null)
                _repo.Delete(photoFromRepo);  

            if(await _repo.SaveAll())
                return Ok();

            return BadRequest("Failed to delete the photo");       
            
        }

    }
}
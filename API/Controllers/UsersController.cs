using System.Security.Claims;
using API.DTOs;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Company.ClassLibrary1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers;
#nullable disable

[AllowAnonymous]

public class UsersController : BaseApiController
{
    
    private readonly IUserRepository _userRepository;
    
    private readonly IMapper _mapper;
    private readonly IImageService _imageService;

    public UsersController(IImageService imageService,IUserRepository userRepository, IMapper mapper)
    {
        this._userRepository = userRepository;

        _mapper = mapper;

        _imageService = imageService;

    }
    private async Task<AppUser> _GetUser(){
        var username = User.GetUsername();
        if (username is null) return null;
        return await _userRepository.GetUserByUserNameAsync(username);
    }

    [HttpGet]
    public async Task<ActionResult<PageList<MemberDto>>> GetUsers([FromQuery] UserParams userParams)   
    {
        var username = User.GetUsername();
        if (username is null) return NotFound();

        var currentUser = await _userRepository.GetUserByUserNameAsync(username);
        if (currentUser is null) return NotFound();
        userParams.CurrentUserName = currentUser.UserName;
        if (string.IsNullOrEmpty(userParams.Gender))
        {
            if (currentUser.Gender != "non-binary")
                userParams.Gender = currentUser.Gender == "male" ? "female" : "male";
            else
                userParams.Gender = "non-binary";
        }

        var pages = await _userRepository.GetMembersAsync(userParams);
        Response.AddPaginationHeader(
            new PaginationHeader(pages.CurrentPage, pages.PageSize, pages.TotalCount, pages.TotalPages));
        return Ok(pages);
    }


    [HttpGet("{id}")]
     public async Task<ActionResult<MemberDto>> GetUser(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        return _mapper.Map<MemberDto>(user);
    }


    [HttpGet("username/{username}")]
    public async Task<ActionResult<MemberDto>> GetUserByUserName(string username)
    {
        return await _userRepository.GetMemberByUsernameAsync(username);
    }
    [HttpPut]
    public async Task<ActionResult> UpdateUserProfile(MemberUpdateDto memberUpdateDto)
    {
        var appuser = await _GetUser();
        if (appuser is null) return NotFound();

        _mapper.Map(memberUpdateDto, appuser);
        if (await _userRepository.SaveAllAsync()) return NoContent();

        return BadRequest("Failed to update user profile!");
    }
    [HttpPost("add-image")]
    public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
    {
        var user = await _GetUser();
        if (user is null) return NotFound();

        var result = await _imageService.AddImageAsync(file);
        if (result.Error is not null) return BadRequest(result.Error.Message);

        var photo = new Photo
        {
            Url = result.SecureUrl.AbsoluteUri,
            PublicId = result.PublicId
        };
        if (user.Photos.Count == 0) photo.IsMain = true;

        user.Photos.Add(photo);
        if (await _userRepository.SaveAllAsync()) return CreatedAtAction( //status 201
                nameof(GetUserByUserName),
                new { username = user.UserName },
                _mapper.Map<PhotoDto>(photo)
            );
        return BadRequest("Something has gone wrong!");
    }
        [HttpPut("set-main-photo/{photoId}")]
    public async Task<ActionResult> SetMainPhoto(int photoId)
    {
        var user = await _GetUser();
        if (user is null) return NotFound();

        var photo = user.Photos.FirstOrDefault(photo => photo.Id == photoId);
        if (photo is null) return NotFound();

        if (photo.IsMain) return BadRequest("this photo(id:" + photo.Id + ") is already main photo");

        var currentMainPhoto = user.Photos.FirstOrDefault(photo => photo.IsMain == true);
        if (currentMainPhoto is not null) currentMainPhoto.IsMain = false;
        photo.IsMain = true;

        if (await _userRepository.SaveAllAsync()) return NoContent();

        return BadRequest("Something has gone wrong!");
    }

    [HttpDelete("delete-photo/{photoId}")]
    public async Task<ActionResult> DeletePhoto(int photoId)
    {
        var user = await _GetUser();
        if (user is null) return NotFound();

        var photo = user.Photos.FirstOrDefault(photo => photo.Id == photoId);
        if (photo is null) return NotFound();

        if (photo.IsMain) return BadRequest("can't delete main photo");

        if (photo.PublicId is not null)
        {
            var result = await _imageService.DeleteImageAsync(photo.PublicId);
            if (result.Error is not null) return BadRequest(result.Error.Message);
        }

        user.Photos.Remove(photo);
        if (await _userRepository.SaveAllAsync()) return NoContent();

        return BadRequest("Something has gone wrong!");
    }
}
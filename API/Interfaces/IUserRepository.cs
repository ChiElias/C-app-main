﻿using API.Helpers;
using Company.ClassLibrary1;

namespace API.Interfaces;

public interface IUserRepository
{
    void Update(AppUser user);
    Task<bool> SaveAllAsync();
    Task<AppUser?> GetUserByIdAsync(int id);
    Task<AppUser?> GetUserByUserNameAsync(string username);
    // Task<IEnumerable<AppUser>> GetUsersAsync();
    // Task<IEnumerable<MemberDto>> GetMembersAsync();
    Task<PageList<MemberDto>> GetMembersAsync(UserParams userParams);
    Task<MemberDto?> GetMemberByUsernameAsync(string username);
    Task<AppUser?> GetUserByUserNameWithOutPhotoAsync(string username);

}
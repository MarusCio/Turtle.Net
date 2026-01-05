
namespace Turtle.Models.ViewModels
{
    public class UserProfileViewModel
{
    public ApplicationUser? User { get; set; }

    public List<Post>? Posts { get; set; } = [];
    public List<Post>? LikedPosts { get; set; } = [];
    public List<UserCommunity>? Communities { get; set; } = [];

    public List<ApplicationUser>? Followers { get; set; } = [];
    public List<ApplicationUser>? Following { get; set; } = [];


    public int? FollowerCount;

    public bool? IsCurrentUser { get; set; }
    public bool? IsFollowing { get; set; }
}
}

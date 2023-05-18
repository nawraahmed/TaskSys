using TaskManagementSystem.Models;

namespace TaskManagementSystem.ViewModels
{
    public class ProjectsUsersVM
    {
        public Project Project { get; set; }
        public IEnumerable<User> Users { get; set; }
    }
}

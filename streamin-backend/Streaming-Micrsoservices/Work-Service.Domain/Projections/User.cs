using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Work_Service.Domain.Abstraction;
using Work_Service.Domain.ProjectContext;

namespace Work_Service.Domain.Projections
{
    public class User
    {
        private User() { }
        public Guid Userid {  get; init; }

        public Role role { get; init; }

        public string email { get; init; }

        private User(Guid userid, Role role, string email)
        {
            Userid = userid;
            this.role = role;
            this.email = email;
        }

        public static User CreateUser(Guid userid, Role role, string email)
        {
            var user=new User(userid, role, email);
            return user;
        }
    }
}

using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Work_Service.Application.Abstractions;
using Work_Service.Domain.Abstraction;
using Work_Service.Domain.ProjectContext;
using Work_Service.Domain.Projections;

namespace Work_Service.Application.Projections
{
        public record UserCreateRequestCommand(string email, Guid userId, string membership):IRequest{}

        public class UserCreateCommandHandler: IRequestHandler<UserCreateRequestCommand>
        {
            private readonly IUnitofWork<User> _unitOfWork;
            public UserCreateCommandHandler(IUnitofWork<User> unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task Handle(UserCreateRequestCommand request, CancellationToken cancellationToken)
            {
            
                User user = User.CreateUser(request.userId, Role.Employee, request.email);
                _unitOfWork.Add(user);
                await _unitOfWork.SaveChangesAsync();
                
            
            }
        }
    
}

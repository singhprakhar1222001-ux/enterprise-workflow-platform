using Contracts.WorkService.Consts;
using Contracts.WorkService.Events;
using Contracts.WorkService.RoutingEventDirectory;
using Elastic.Clients.Elasticsearch;
using MediatR;
using Newtonsoft.Json;
using RabbitMQ.Client;
using SearchService.API.IndexClass;
using SearchService.API.Infrastructure.Buffer;
using SearchService.API.Infrastructure.Cache;
using SearchService.API.Infrastructure.Messaging.Connection;
using SearchService.API.Infrastructure.Messaging.Topology;
using SearchService.API.Infrastructure.Projections.Models;
using System.Text;
using System.Text.Unicode;

namespace SearchService.API.Features.AddIndex
{

    public class WorkIndexCommandHandler : INotificationHandler<WorkCreatedEvent>
    {
        private readonly ProjectCache _projectCache;
        private readonly ProjectUserCache _projectUserCache;
        private readonly MessageBuffer _messageBuffer;
        private readonly IConnectionManager _connectionManager;
        public WorkIndexCommandHandler(ProjectUserCache projectUserCache, ProjectCache projectCache, MessageBuffer messageBuffer,IConnectionManager connection)
        {
            _projectCache = projectCache;
            _projectUserCache = projectUserCache;
            _messageBuffer = messageBuffer;
            _connectionManager= connection;
        }
        public async Task Handle(WorkCreatedEvent request, CancellationToken cancellationToken)
        {
            var UserId = request.comment.OrderByDescending(x => x.Timestamp).FirstOrDefault().UserId;
            try
            {
                //validate for the presence of both in it
                if (UserId != null && await _projectCache.GetValue(request.ProjectId) != null && await _projectUserCache.GetValue(request.managerId)!=null && await _projectUserCache.GetValue(request.assignedId)!=null)
                {
                    ProjectProjection project = new();
                    //bool resProject = await _projectCache.GetValue(request.ProjectId);
                    //ProjectUserProjection projectAssignedUser = new();
                    //ProjectUserProjection projectManager = new ProjectUserProjection();
                    ProjectUserProjection? projectAssignedUser = await _projectUserCache.GetValue(request.assignedId);
                    ProjectUserProjection? projectManager = await _projectUserCache.GetValue(request.managerId);
                    List<Comments> comments = new();
                    List<CommentEventProperty> commentsInRequest = request.comment;
                    var commentsInIndexBody = await Task.WhenAll(
                        commentsInRequest.Select(async x =>
                        {
                            var comment = new Comments
                            {
                                comment = x.comment,
                                UserId = x.UserId,
                                UserName = (await _projectUserCache.GetValue(x.UserId))?.UserName ?? "Unknown User",
                                Timestamp = x.Timestamp
                            };
                            return comment;
                        })
                    );
                    var commentList = commentsInIndexBody.ToList();
                    WorkIndexBody body = new WorkIndexBody
                    {
                        Id = request.Id,
                        Name = request.Name,
                        ProjectId = request.ProjectId,
                        ProjectName = project.ProjectName,
                        _Comment = commentList,
                        assignedId = request.assignedId,
                        AssignedName = projectAssignedUser.UserName,
                        ManagerName = projectManager.UserName,
                        managerId = request.managerId,
                        description = request.description,
                        Deadline = request.deadline,
                        AssignmentDate = request.AssignmentDate,
                        WorkStatus = request.WorkStatus,
                        IsOverDue = request.IsOverDue,
                        Version = request.Version,
                    };
                    await _messageBuffer.AddMessage(body);

                }
                else
                {
                    //send his ass to cancun (Retry queue)
                    var connection = await _connectionManager.GetConnection();
                    using var channel = await connection.CreateChannelAsync();
                    var body_string = JsonConvert.SerializeObject(request);
                    byte[] body = UTF8Encoding.UTF8.GetBytes(body_string);
                    //just get the key in the way you were getting it
                    string routingkey = RoutingEventDirectory.GetRoutingKey(request.GetType());
                    await channel.BasicPublishAsync(
                        exchange: Topology.retryExchange30s,
                        routingKey: routingkey,
                        body: body,
                        cancellationToken: cancellationToken
                        );
                    return;
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
}
}

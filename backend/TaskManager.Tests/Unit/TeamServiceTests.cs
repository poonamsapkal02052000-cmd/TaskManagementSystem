using TaskManager.Api.Common;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;
using TaskManager.Api.Services;

namespace TaskManager.Tests.Unit;

public class TeamServiceTests : IDisposable
{
    private readonly TestDb _t = new();
    private TeamService Svc() => new(_t.Db);
    private CurrentUser As(User u) => new(u.Id, u.Role);

    [Fact]
    public async Task Manager_can_add_unassigned_user_to_own_team()
    {
        var team = await Svc().AddMemberAsync(As(_t.Manager), _t.Team.Id, _t.Outsider.Id);
        Assert.Contains(team.Members, m => m.Id == _t.Outsider.Id);
    }

    [Fact]
    public async Task Manager_cannot_change_members_of_another_team()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Svc().AddMemberAsync(As(_t.OtherManager), _t.Team.Id, _t.Outsider.Id));
    }

    [Fact]
    public async Task Team_manager_must_have_manager_role()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Svc().CreateAsync(new CreateTeamRequest("QA", null, _t.Member.Id)));
    }

    [Fact]
    public async Task Team_names_are_unique()
    {
        await Assert.ThrowsAsync<ConflictException>(() =>
            Svc().CreateAsync(new CreateTeamRequest("Core", null, null)));
    }

    [Fact]
    public async Task User_sees_only_their_own_team()
    {
        var other = await Svc().CreateAsync(new CreateTeamRequest("Other", null, _t.OtherManager.Id));

        var teams = await Svc().GetTeamsAsync(As(_t.Member));

        Assert.Equal(_t.Team.Id, Assert.Single(teams).Id);
        await Assert.ThrowsAsync<ForbiddenException>(() => Svc().GetTeamAsync(As(_t.Member), other.Id));
    }

    public void Dispose() => _t.Dispose();
}

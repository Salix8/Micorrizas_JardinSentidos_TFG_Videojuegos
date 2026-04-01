using System;
using System.Linq;
using System.Threading.Tasks;
using SmartCampus.Coop;

namespace SmartCampus.Testing.Editor.Definitions
{
    public abstract class ManagerStateQaTestCase : ProjectQaTestCase
    {
        public override string CategoryId => "managers_state";
        public override string CategoryName => "Managers / Global State";
        public override int CategoryOrder => 2;
    }

    public sealed class CoopScriptsPreventDuplicatesQaTest : ManagerStateQaTestCase
    {
        public override string Id => "managers.disallow_multiple_component";
        public override string DisplayName => "Co-op scripts prevent duplicate components";
        public override string Description => "Checks the key multiplayer MonoBehaviours remain protected by DisallowMultipleComponent.";
        public override int Order => 0;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var protectedTypes = new[]
            {
                typeof(RelayConnectionService),
                typeof(CoopSessionCoordinator),
                typeof(MultiplayerMenuController),
                typeof(CoopInformationPresenter)
            };

            var missingProtection = protectedTypes
                .Where(type => !Attribute.IsDefined(type, typeof(UnityEngine.DisallowMultipleComponent)))
                .Select(type => type.Name)
                .ToArray();

            if (missingProtection.Length > 0)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "One or more cooperative scripts lost duplicate-component protection.",
                    string.Join(", ", missingProtection)));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "The cooperative scripts remain protected against duplicate components.",
                string.Join(", ", protectedTypes.Select(type => type.Name))));
        }
    }

    public sealed class RelayServicePersistsAcrossScenesQaTest : ManagerStateQaTestCase
    {
        public override string Id => "managers.relay_persists_across_scenes";
        public override string DisplayName => "Relay service persists across scenes";
        public override string Description => "Confirms the lobby bootstrap is configured to survive the transition into the main map.";
        public override int Order => 1;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var persists = SmartCampusProjectQaUtility.InspectScene(
                SmartCampusProjectQaUtility.LobbyScenePath,
                scene =>
                {
                    var relayService = SmartCampusProjectQaUtility.FindComponents<RelayConnectionService>(scene).FirstOrDefault();
                    return relayService != null && SmartCampusProjectQaUtility.ReadBool(relayService, "persistAcrossScenes");
                });

            if (!persists)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "RelayConnectionService is not configured to persist across scene loads.",
                    "Enable the persistAcrossScenes flag in the Lobby scene if the manager must survive the transition to UJI."));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "RelayConnectionService persists across scene loads.",
                "persistAcrossScenes = true"));
        }
    }

    public sealed class LobbyRulesStayAlignedQaTest : ManagerStateQaTestCase
    {
        public override string Id => "managers.lobby_rules_aligned";
        public override string DisplayName => "Relay and coordinator rules stay aligned";
        public override string Description => "Prevents scene serialization drift between RelayConnectionService and CoopSessionCoordinator.";
        public override int Order => 2;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var report = SmartCampusProjectQaUtility.InspectScene(
                SmartCampusProjectQaUtility.LobbyScenePath,
                scene =>
                {
                    var relayService = SmartCampusProjectQaUtility.FindComponents<RelayConnectionService>(scene).FirstOrDefault();
                    var coordinator = SmartCampusProjectQaUtility.FindComponents<CoopSessionCoordinator>(scene).FirstOrDefault();
                    return new
                    {
                        RelayMin = relayService == null ? 0 : SmartCampusProjectQaUtility.ReadInt(relayService, "minPlayersToStart"),
                        RelayMax = relayService == null ? 0 : SmartCampusProjectQaUtility.ReadInt(relayService, "maxPlayers"),
                        CoordinatorMin = coordinator == null ? 0 : SmartCampusProjectQaUtility.ReadInt(coordinator, "minPlayersToStart"),
                        CoordinatorMax = coordinator == null ? 0 : SmartCampusProjectQaUtility.ReadInt(coordinator, "maxPlayers"),
                        CoordinatorLobby = coordinator == null ? string.Empty : SmartCampusProjectQaUtility.ReadString(coordinator, "lobbySceneName"),
                        CoordinatorMainMap = coordinator == null ? string.Empty : SmartCampusProjectQaUtility.ReadString(coordinator, "mainMapSceneName"),
                        RelayMainMap = relayService == null ? string.Empty : SmartCampusProjectQaUtility.ReadString(relayService, "mainMapSceneName")
                    };
                });

            var valid = report.RelayMin == CoopSessionRules.DefaultMinimumPlayers &&
                        report.RelayMax == CoopSessionRules.DefaultMaximumPlayers &&
                        report.CoordinatorMin == CoopSessionRules.DefaultMinimumPlayers &&
                        report.CoordinatorMax == CoopSessionRules.DefaultMaximumPlayers &&
                        report.CoordinatorLobby == SmartCampusProjectQaUtility.LobbySceneName &&
                        report.CoordinatorMainMap == SmartCampusProjectQaUtility.MainMapSceneName &&
                        report.RelayMainMap == SmartCampusProjectQaUtility.MainMapSceneName;
            var details = $"Relay: {report.RelayMin}-{report.RelayMax}\nCoordinator: {report.CoordinatorMin}-{report.CoordinatorMax}\nLobby scene: {report.CoordinatorLobby}\nCoordinator main map: {report.CoordinatorMainMap}\nRelay main map: {report.RelayMainMap}";

            if (!valid)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The cooperative rules or scene names are out of sync between managers.",
                    details));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "RelayConnectionService and CoopSessionCoordinator stay aligned on rules and scene names.",
                details));
        }
    }

    public sealed class MultiplayerMenuReferencesAssignedQaTest : ManagerStateQaTestCase
    {
        public override string Id => "managers.menu_references_assigned";
        public override string DisplayName => "Lobby menu references are assigned";
        public override string Description => "Verifies the critical UI references required by MultiplayerMenuController remain wired in the Lobby scene.";
        public override int Order => 3;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var report = SmartCampusProjectQaUtility.InspectScene(
                SmartCampusProjectQaUtility.LobbyScenePath,
                scene =>
                {
                    var menu = SmartCampusProjectQaUtility.FindComponents<MultiplayerMenuController>(scene).FirstOrDefault();
                    return new
                    {
                        HasMenu = menu != null,
                        HomePanel = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "homePanel"),
                        HostPanel = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "hostPanel"),
                        JoinPanel = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "joinPanel"),
                        SessionPanel = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "sessionPanel"),
                        JoinCodeInput = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "joinCodeInput"),
                        StatusLabel = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "statusLabel"),
                        JoinCodeLabel = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "joinCodeLabel"),
                        PlayerCountLabel = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "playerCountLabel"),
                        RequirementsLabel = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "sessionRequirementsLabel"),
                        HostButton = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "hostButton"),
                        JoinButton = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "joinButton"),
                        StartButton = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "startMatchButton"),
                        LeaveButton = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "leaveSessionButton"),
                        CopyButton = menu != null && SmartCampusProjectQaUtility.HasAssignedReference(menu, "copyJoinCodeButton")
                    };
                });

            var valid = report.HasMenu &&
                        report.HomePanel &&
                        report.HostPanel &&
                        report.JoinPanel &&
                        report.SessionPanel &&
                        report.JoinCodeInput &&
                        report.StatusLabel &&
                        report.JoinCodeLabel &&
                        report.PlayerCountLabel &&
                        report.RequirementsLabel &&
                        report.HostButton &&
                        report.JoinButton &&
                        report.StartButton &&
                        report.LeaveButton &&
                        report.CopyButton;
            var details = $"HomePanel={report.HomePanel}, HostPanel={report.HostPanel}, JoinPanel={report.JoinPanel}, SessionPanel={report.SessionPanel}, JoinCodeInput={report.JoinCodeInput}, StatusLabel={report.StatusLabel}, JoinCodeLabel={report.JoinCodeLabel}, PlayerCountLabel={report.PlayerCountLabel}, RequirementsLabel={report.RequirementsLabel}, HostButton={report.HostButton}, JoinButton={report.JoinButton}, StartButton={report.StartButton}, LeaveButton={report.LeaveButton}, CopyButton={report.CopyButton}";

            if (!valid)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "MultiplayerMenuController has missing scene references.",
                    details));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "MultiplayerMenuController keeps its critical lobby references assigned.",
                details));
        }
    }
}

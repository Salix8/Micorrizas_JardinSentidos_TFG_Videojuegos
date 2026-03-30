using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;

namespace SmartCampus.Testing.Editor.Definitions
{
    public abstract class SceneFlowQaTestCase : ProjectQaTestCase
    {
        public override string CategoryId => "scene_flow";
        public override string CategoryName => "Scene Flow";
        public override int CategoryOrder => 1;
    }

    public sealed class LobbySceneIncludedInBuildSettingsQaTest : SceneFlowQaTestCase
    {
        public override string Id => "scene.lobby_in_build_settings";
        public override string DisplayName => "Lobby scene is enabled in Build Settings";
        public override string Description => "Required for synchronized scene changes back to the lobby to work in builds.";
        public override int Order => 0;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            if (!SmartCampusProjectQaUtility.SceneExists(SmartCampusProjectQaUtility.LobbyScenePath))
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The lobby scene asset does not exist.",
                    SmartCampusProjectQaUtility.LobbyScenePath));
            }

            if (!SmartCampusProjectQaUtility.IsSceneEnabled(SmartCampusProjectQaUtility.LobbyScenePath))
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The lobby scene is not enabled in Build Settings.",
                    $"Enable {SmartCampusProjectQaUtility.LobbyScenePath} so the cooperative flow can return to the lobby in builds."));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "The lobby scene is enabled in Build Settings.",
                SmartCampusProjectQaUtility.LobbyScenePath));
        }
    }

    public sealed class MainMapSceneIncludedInBuildSettingsQaTest : SceneFlowQaTestCase
    {
        public override string Id => "scene.main_map_in_build_settings";
        public override string DisplayName => "Main map scene is enabled in Build Settings";
        public override string Description => "Required for the host to transition the group into the main cooperative map.";
        public override int Order => 1;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            if (!SmartCampusProjectQaUtility.SceneExists(SmartCampusProjectQaUtility.MainMapScenePath))
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The main map scene asset does not exist.",
                    SmartCampusProjectQaUtility.MainMapScenePath));
            }

            if (!SmartCampusProjectQaUtility.IsSceneEnabled(SmartCampusProjectQaUtility.MainMapScenePath))
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The main map scene is not enabled in Build Settings.",
                    $"Enable {SmartCampusProjectQaUtility.MainMapScenePath} so the cooperative flow can load the main map in builds."));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "The main map scene is enabled in Build Settings.",
                SmartCampusProjectQaUtility.MainMapScenePath));
        }
    }

    public sealed class LobbySceneContainsBootstrapQaTest : SceneFlowQaTestCase
    {
        public override string Id => "scene.lobby_contains_bootstrap";
        public override string DisplayName => "Lobby scene contains the multiplayer bootstrap";
        public override string Description => "Checks that the lobby scene has one NetworkManager, one RelayConnectionService, one coordinator and one menu controller.";
        public override int Order => 2;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var report = SmartCampusProjectQaUtility.InspectScene(
                SmartCampusProjectQaUtility.LobbyScenePath,
                scene => new
                {
                    NetworkManagers = SmartCampusProjectQaUtility.FindComponents<NetworkManager>(scene).Count,
                    RelayServices = SmartCampusProjectQaUtility.FindComponents<RelayConnectionService>(scene).Count,
                    Coordinators = SmartCampusProjectQaUtility.FindComponents<CoopSessionCoordinator>(scene).Count,
                    MenuControllers = SmartCampusProjectQaUtility.FindComponents<MultiplayerMenuController>(scene).Count
                });

            var isValid = report.NetworkManagers == 1 &&
                          report.RelayServices == 1 &&
                          report.Coordinators == 1 &&
                          report.MenuControllers == 1;
            var details = $"NetworkManager: {report.NetworkManagers}, RelayConnectionService: {report.RelayServices}, CoopSessionCoordinator: {report.Coordinators}, MultiplayerMenuController: {report.MenuControllers}";

            if (!isValid)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The lobby scene multiplayer bootstrap is incomplete or duplicated.",
                    details));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "The lobby scene contains a single multiplayer bootstrap stack.",
                details));
        }
    }

    public sealed class MainMapSceneDoesNotDuplicateBootstrapQaTest : SceneFlowQaTestCase
    {
        public override string Id => "scene.main_map_has_no_duplicate_bootstrap";
        public override string DisplayName => "Main map scene does not duplicate persistent managers";
        public override string Description => "Prevents duplicate network/bootstrap objects once the persistent lobby services move into the main map.";
        public override int Order => 3;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var report = SmartCampusProjectQaUtility.InspectScene(
                SmartCampusProjectQaUtility.MainMapScenePath,
                scene => new
                {
                    NetworkManagers = SmartCampusProjectQaUtility.FindComponents<NetworkManager>(scene).Count,
                    RelayServices = SmartCampusProjectQaUtility.FindComponents<RelayConnectionService>(scene).Count,
                    Coordinators = SmartCampusProjectQaUtility.FindComponents<CoopSessionCoordinator>(scene).Count,
                    MenuControllers = SmartCampusProjectQaUtility.FindComponents<MultiplayerMenuController>(scene).Count
                });

            var isValid = report.NetworkManagers == 0 &&
                          report.RelayServices == 0 &&
                          report.Coordinators == 0 &&
                          report.MenuControllers == 0;
            var details = $"NetworkManager: {report.NetworkManagers}, RelayConnectionService: {report.RelayServices}, CoopSessionCoordinator: {report.Coordinators}, MultiplayerMenuController: {report.MenuControllers}";

            if (!isValid)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The main map scene duplicates managers that should arrive from the lobby bootstrap.",
                    details));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "The main map scene leaves the persistent multiplayer bootstrap to the lobby scene.",
                details));
        }
    }

    public sealed class CooperativeSceneNamesResolveQaTest : SceneFlowQaTestCase
    {
        public override string Id => "scene.cooperative_scene_names_resolve";
        public override string DisplayName => "Configured cooperative scene names resolve";
        public override string Description => "Confirms the scene names configured in the lobby flow match real assets.";
        public override int Order => 4;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var report = SmartCampusProjectQaUtility.InspectScene(
                SmartCampusProjectQaUtility.LobbyScenePath,
                scene =>
                {
                    var coordinator = SmartCampusProjectQaUtility.FindComponents<CoopSessionCoordinator>(scene).FirstOrDefault();
                    var relayService = SmartCampusProjectQaUtility.FindComponents<RelayConnectionService>(scene).FirstOrDefault();
                    return new
                    {
                        LobbySceneName = coordinator == null ? string.Empty : SmartCampusProjectQaUtility.ReadString(coordinator, "lobbySceneName"),
                        MainMapSceneName = coordinator == null ? string.Empty : SmartCampusProjectQaUtility.ReadString(coordinator, "mainMapSceneName"),
                        RelayMainMapSceneName = relayService == null ? string.Empty : SmartCampusProjectQaUtility.ReadString(relayService, "mainMapSceneName")
                    };
                });

            var valid = report.LobbySceneName == SmartCampusProjectQaUtility.LobbySceneName &&
                        report.MainMapSceneName == SmartCampusProjectQaUtility.MainMapSceneName &&
                        report.RelayMainMapSceneName == SmartCampusProjectQaUtility.MainMapSceneName;
            var details = $"Coordinator lobby scene: {report.LobbySceneName}\nCoordinator main map scene: {report.MainMapSceneName}\nRelay main map scene: {report.RelayMainMapSceneName}";

            if (!valid)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The cooperative scene names do not resolve to the expected assets.",
                    details));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "The cooperative scene names resolve to the expected assets.",
                details));
        }
    }
}

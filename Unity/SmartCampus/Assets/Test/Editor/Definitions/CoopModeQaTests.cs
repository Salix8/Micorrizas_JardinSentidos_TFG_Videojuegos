using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartCampus.Coop;

namespace SmartCampus.Testing.Definitions
{
    public abstract class CoopModeQaTestCase : ProjectQaTestCase
    {
        public override string CategoryId => "coop_mode";
        public override string CategoryName => "Cooperative Mode / Relay / Lobby / Network";
        public override int CategoryOrder => 0;

        internal static SimulatedCoopEnvironment CreateEnvironment()
        {
            return new SimulatedCoopEnvironment(new CoopSessionRules(
                CoopSessionRules.DefaultMinimumPlayers,
                CoopSessionRules.DefaultMaximumPlayers));
        }

        internal static string DescribeSnapshot(CoopSessionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "No synchronized lobby snapshot is available.";
            }

            var players = string.Join(", ", snapshot.Players.Select(player =>
                $"{player.SlotIndex}:{player.DeviceId}{(player.IsHost ? " (host)" : string.Empty)}"));
            return $"Lobby: {snapshot.LobbyCode}\nPhase: {snapshot.Phase}\nPlayers: {snapshot.PlayerCount}\nMembers: {players}";
        }
    }

    public abstract class PlayerValidationQaTestCase : ProjectQaTestCase
    {
        public override string CategoryId => "players_limits";
        public override string CategoryName => "Players / Limits / Session Consistency";
        public override int CategoryOrder => 3;
    }

    public sealed class CoopHostCreatesLobbyQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.host_creates_lobby";
        public override string DisplayName => "Host creates a lobby successfully";
        public override string Description => "Validates simulated host bootstrap, join code creation and initial lobby state.";
        public override int Order => 0;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CoopModeQaTestCase.CreateEnvironment();
            var result = environment.CreateHost("Host-A");
            context.Info(result.Message);

            if (!result.Success || string.IsNullOrWhiteSpace(result.LobbyCode) || result.Snapshot == null || result.Snapshot.PlayerCount != 1)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The host could not create a valid lobby snapshot.",
                    result.Message));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                $"Host created lobby {result.LobbyCode}.",
                DescribeSnapshot(result.Snapshot)));
        }
    }

    public sealed class CoopClientJoinSuccessQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.client_joins_lobby";
        public override string DisplayName => "Second device joins the lobby";
        public override string Description => "Confirms the common host + client flow works with synchronized player counts.";
        public override int Order => 1;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CoopModeQaTestCase.CreateEnvironment();
            var host = environment.CreateHost("Host-A");
            var join = environment.Join("Client-B", host.LobbyCode);
            context.Info(host.Message);
            context.Info(join.Message);

            if (!join.Success || join.Snapshot == null || join.Snapshot.PlayerCount != 2)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The second device did not join the lobby correctly.",
                    join.Message));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "Host and client share the same 2-player lobby state.",
                DescribeSnapshot(join.Snapshot)));
        }
    }

    public sealed class CoopStartRejectedBelowMinimumQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.start_rejected_below_minimum";
        public override string DisplayName => "Session cannot start with fewer than 2 players";
        public override string Description => "Checks the project acceptance rule that a single host is not enough to launch the cooperative session.";
        public override int Order => 2;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CoopModeQaTestCase.CreateEnvironment();
            var host = environment.CreateHost("Host-A");
            var start = environment.StartMainMap("Host-A");
            context.Info(host.Message);
            context.Warning(start.Message);

            if (start.Success)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The lobby started even though only one player was connected.",
                    DescribeSnapshot(start.Snapshot)));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "The lobby correctly blocks session start below the 2-player minimum.",
                start.Message));
        }
    }

    public sealed class CoopStartAllowedWithinRangeQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.start_allowed_two_to_six";
        public override string DisplayName => "Session starts correctly with 2 to 6 players";
        public override string Description => "Sweeps the supported player range and confirms the host can start the session for every accepted population.";
        public override int Order => 3;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var validatedCounts = new List<int>();

            for (var playerCount = CoopSessionRules.DefaultMinimumPlayers; playerCount <= CoopSessionRules.DefaultMaximumPlayers; playerCount++)
            {
                var environment = CreateEnvironment();
                var host = environment.CreateHost("Host-A");
                context.Info(host.Message);

                for (var index = 1; index < playerCount; index++)
                {
                    var join = environment.Join($"Client-{index}", host.LobbyCode);
                    if (!join.Success)
                    {
                        return Task.FromResult(ProjectQaOutcome.Fail(
                            $"The simulated client flow broke while preparing the {playerCount}-player session.",
                            join.Message));
                    }
                }

                var start = environment.StartMainMap("Host-A");
                if (!start.Success)
                {
                    return Task.FromResult(ProjectQaOutcome.Fail(
                        $"The host could not start a valid {playerCount}-player session.",
                        start.Message));
                }

                validatedCounts.Add(playerCount);
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "The cooperative session starts for every supported population between 2 and 6 players.",
                $"Validated populations: {string.Join(", ", validatedCounts)}"));
        }
    }

    public sealed class CoopRejectsSeventhPlayerQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.rejects_seventh_player";
        public override string DisplayName => "Joining a seventh player is rejected";
        public override string Description => "Validates the upper limit of 6 players and keeps the lobby state unchanged after rejection.";
        public override int Order => 4;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CoopModeQaTestCase.CreateEnvironment();
            var host = environment.CreateHost("Host-A");

            for (var index = 1; index < CoopSessionRules.DefaultMaximumPlayers; index++)
            {
                var join = environment.Join($"Client-{index}", host.LobbyCode);
                if (!join.Success)
                {
                    return Task.FromResult(ProjectQaOutcome.Fail(
                        "The setup phase could not reach the 6-player limit before the rejection test.",
                        join.Message));
                }
            }

            var overflow = environment.Join("Client-Overflow", host.LobbyCode);
            context.Warning(overflow.Message);

            if (overflow.Success || overflow.FailureKind != SimulatedCoopFailureKind.LobbyFull)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The lobby accepted more than 6 players or returned the wrong rejection reason.",
                    overflow.Message));
            }

            var finalSnapshot = environment.GetSnapshot("Host-A");
            return Task.FromResult(ProjectQaOutcome.Pass(
                "The lobby rejects a seventh player and preserves the 6-player session.",
                DescribeSnapshot(finalSnapshot)));
        }
    }

    public sealed class CoopMissingJoinFailureSurfacedQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.join_failure_surfaced";
        public override string DisplayName => "A failed join is surfaced when nobody reaches the host";
        public override string Description => "Ensures the tool can flag the failure path where another device never joins the host lobby.";
        public override int Order => 5;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CoopModeQaTestCase.CreateEnvironment();
            var host = environment.CreateHost("Host-A");
            environment.ConfigureNextJoinFailure(
                SimulatedCoopFailureKind.ConnectionError,
                "No client could reach the host lobby.");
            var join = environment.Join("Client-B", host.LobbyCode);
            context.Info(host.Message);
            context.Warning(join.Message);

            if (join.Success || join.FailureKind != SimulatedCoopFailureKind.ConnectionError)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The tool did not surface the missing-client join failure correctly.",
                    join.Message));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "Join failures are surfaced as expected when no client reaches the host.",
                join.Message));
        }
    }

    public sealed class CoopInvalidCodeRejectedQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.invalid_code_rejected";
        public override string DisplayName => "Invalid lobby codes are rejected";
        public override string Description => "Rejects malformed or unknown join codes before the session mutates.";
        public override int Order => 6;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CoopModeQaTestCase.CreateEnvironment();
            var join = environment.Join("Client-B", "INVALID");
            context.Warning(join.Message);

            if (join.Success || join.FailureKind != SimulatedCoopFailureKind.InvalidCode)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The simulator accepted an invalid lobby code.",
                    join.Message));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "Invalid lobby codes are rejected cleanly.",
                join.Message));
        }
    }

    public sealed class CoopJoinTimeoutHandledQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.timeout_handled";
        public override string DisplayName => "Join timeouts are handled cleanly";
        public override string Description => "Exercises the controlled timeout path without relying on the real Relay service.";
        public override int Order => 7;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CoopModeQaTestCase.CreateEnvironment();
            var host = environment.CreateHost("Host-A");
            environment.ConfigureNextJoinFailure(
                SimulatedCoopFailureKind.Timeout,
                "Join operation timed out before the client connected.");
            var join = environment.Join("Client-B", host.LobbyCode);
            context.Info(host.Message);
            context.Warning(join.Message);

            if (join.Success || join.FailureKind != SimulatedCoopFailureKind.Timeout)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The timeout path did not report the expected failure.",
                    join.Message));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "Timeouts are surfaced in a controlled way.",
                join.Message));
        }
    }

    public sealed class CoopConnectionErrorHandledQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.connection_error_handled";
        public override string DisplayName => "Connection errors are handled cleanly";
        public override string Description => "Separates connection errors from generic invalid-code failures for clearer diagnostics.";
        public override int Order => 8;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CreateEnvironment();
            var host = environment.CreateHost("Host-A");
            environment.ConfigureNextJoinFailure(
                SimulatedCoopFailureKind.ConnectionError,
                "The simulated transport dropped the connection.");
            var join = environment.Join("Client-B", host.LobbyCode);
            context.Info(host.Message);
            context.Warning(join.Message);

            if (join.Success || join.FailureKind != SimulatedCoopFailureKind.ConnectionError)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The connection error path did not return the expected diagnostic.",
                    join.Message));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "Connection errors are reported separately from other lobby failures.",
                join.Message));
        }
    }

    public sealed class CoopLeaveUpdatesLobbyStateQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.leave_updates_state";
        public override string DisplayName => "Leaving a lobby updates the shared state";
        public override string Description => "Checks that player removal is reflected in the synchronized lobby snapshot.";
        public override int Order => 9;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CreateEnvironment();
            var host = environment.CreateHost("Host-A");
            environment.Join("Client-B", host.LobbyCode);
            environment.Join("Client-C", host.LobbyCode);
            var leave = environment.Leave("Client-B");
            var snapshot = environment.GetSnapshot("Host-A");
            context.Info(host.Message);
            context.Info(leave.Message);

            if (!leave.Success || snapshot == null || snapshot.PlayerCount != 2 || snapshot.Players.Any(player => player.DeviceId == "Client-B"))
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The lobby did not remove the departing player from the shared state.",
                    leave.Message));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "The lobby state updates after a player leaves.",
                DescribeSnapshot(snapshot)));
        }
    }

    public sealed class CoopHostAndClientsStaySynchronizedQaTest : CoopModeQaTestCase
    {
        public override string Id => "coop.host_client_sync";
        public override string DisplayName => "Host and clients remain synchronized";
        public override string Description => "Confirms that after a valid session start every participant sees the same phase and player count.";
        public override int Order => 10;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CreateEnvironment();
            var host = environment.CreateHost("Host-A");
            environment.Join("Client-B", host.LobbyCode);
            environment.Join("Client-C", host.LobbyCode);
            var start = environment.StartMainMap("Host-A");
            context.Info(host.Message);
            context.Info(start.Message);

            if (!start.Success || !environment.AreSynchronized("Host-A", "Client-B", "Client-C"))
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "Host and clients are not synchronized after the session transition.",
                    DescribeSnapshot(start.Snapshot)));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "All simulated peers remain synchronized after the host starts the session.",
                DescribeSnapshot(start.Snapshot)));
        }
    }

    public sealed class PlayerSlotIdsStayUniqueQaTest : PlayerValidationQaTestCase
    {
        public override string Id => "players.slot_ids_unique";
        public override string DisplayName => "Player slots are unique and sequential";
        public override string Description => "Verifies deterministic slot assignment from host to the latest joined client.";
        public override int Order => 0;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CoopModeQaTestCase.CreateEnvironment();
            var host = environment.CreateHost("Host-A");
            environment.Join("Client-B", host.LobbyCode);
            environment.Join("Client-C", host.LobbyCode);
            environment.Join("Client-D", host.LobbyCode);
            var snapshot = environment.GetSnapshot("Host-A");

            var slots = snapshot?.Players.Select(player => player.SlotIndex).ToArray() ?? new int[0];
            var expected = Enumerable.Range(0, slots.Length).ToArray();
            context.Info(DescribeSlots(snapshot));

            if (!slots.SequenceEqual(expected) || slots.Distinct().Count() != slots.Length)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "Player slots are duplicated or no longer sequential.",
                    DescribeSlots(snapshot)));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "Player slots remain unique and sequential.",
                DescribeSlots(snapshot)));
        }

        private static string DescribeSlots(CoopSessionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "No slot snapshot available.";
            }

            return string.Join(", ", snapshot.Players.Select(player => $"{player.DeviceId}=>{player.SlotIndex}"));
        }
    }

    public sealed class DuplicateDeviceJoinRejectedQaTest : PlayerValidationQaTestCase
    {
        public override string Id => "players.duplicate_device_rejected";
        public override string DisplayName => "Duplicate device joins are rejected";
        public override string Description => "Prevents inconsistent session state caused by the same logical device joining twice.";
        public override int Order => 1;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var environment = CoopModeQaTestCase.CreateEnvironment();
            var host = environment.CreateHost("Host-A");
            environment.Join("Client-B", host.LobbyCode);
            var duplicateJoin = environment.Join("Client-B", host.LobbyCode);
            var snapshot = environment.GetSnapshot("Host-A");
            context.Warning(duplicateJoin.Message);

            if (duplicateJoin.Success || duplicateJoin.FailureKind != SimulatedCoopFailureKind.DuplicateDevice || snapshot == null || snapshot.PlayerCount != 2)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The lobby allowed duplicated device state or lost player consistency.",
                    duplicateJoin.Message));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "Duplicate device joins are rejected without corrupting the lobby.",
                CoopModeQaTestCase.DescribeSnapshot(snapshot)));
        }
    }

    public sealed class PlayerRulesStayWithinTwoToSixQaTest : PlayerValidationQaTestCase
    {
        public override string Id => "players.rules_two_to_six";
        public override string DisplayName => "Player rules normalize to the 2-6 range";
        public override string Description => "Locks the cooperative rules to the acceptance range even if future scene values drift out of bounds.";
        public override int Order => 2;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            var rules = new CoopSessionRules(0, 99);
            context.Info($"Normalized rules: {rules.MinimumPlayers}-{rules.MaximumPlayers}");

            var valid = rules.MinimumPlayers == CoopSessionRules.DefaultMinimumPlayers &&
                        rules.MaximumPlayers == CoopSessionRules.DefaultMaximumPlayers &&
                        !rules.CanStart(1) &&
                        rules.CanStart(2) &&
                        rules.CanStart(6) &&
                        !rules.CanStart(7) &&
                        !rules.CanAddPlayer(6);

            if (!valid)
            {
                return Task.FromResult(ProjectQaOutcome.Fail(
                    "The cooperative rules no longer normalize to the supported 2-6 player range.",
                    $"Normalized rules: {rules.MinimumPlayers}-{rules.MaximumPlayers}"));
            }

            return Task.FromResult(ProjectQaOutcome.Pass(
                "The cooperative rules stay locked to the supported 2-6 range.",
                $"Normalized rules: {rules.MinimumPlayers}-{rules.MaximumPlayers}"));
        }
    }

    public sealed class ReadyWorkflowAvailabilityQaTest : PlayerValidationQaTestCase
    {
        public override string Id => "players.ready_workflow_availability";
        public override string DisplayName => "Ready/not-ready flow availability";
        public override string Description => "Marks the current gap explicitly so the panel can distinguish missing functionality from a passing implementation.";
        public override int Order => 3;

        public override Task<ProjectQaOutcome> ExecuteAsync(ProjectQaExecutionContext context)
        {
            context.Warning("Ready/not-ready is not implemented in the current RelayConnectionService, CoopSessionCoordinator or MultiplayerMenuController flow.");
            return Task.FromResult(ProjectQaOutcome.Inconclusive(
                "Ready/not-ready flow is not implemented in the current runtime.",
                "Add readiness state to the session coordinator or a dedicated player-state service when that gameplay flow becomes part of the cooperative lobby."));
        }
    }
}

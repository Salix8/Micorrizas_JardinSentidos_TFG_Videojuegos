using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoopGpsStateSync : NetworkBehaviour
{
    private readonly NetworkList<CoopPlayerGpsState> playerStates = new();

    public NetworkList<CoopPlayerGpsState> PlayerStates => playerStates;

    public event Action StatesChanged;

    public override void OnNetworkSpawn()
    {
        playerStates.OnListChanged += HandlePlayerStatesChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        StatesChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        playerStates.OnListChanged -= HandlePlayerStatesChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    public void SubmitLocalReading(DeviceGpsReading reading)
    {
        if (!IsSpawned || NetworkManager == null || !NetworkManager.IsListening)
        {
            return;
        }

        SubmitGpsStateServerRpc(
            reading.Latitude,
            reading.Longitude,
            reading.Altitude,
            reading.HorizontalAccuracy,
            reading.DeviceTimestamp,
            (int)reading.Status,
            reading.HasFix);
    }

    public bool TryGetState(ulong clientId, out CoopPlayerGpsState state)
    {
        for (var index = 0; index < playerStates.Count; index++)
        {
            if (playerStates[index].ClientId == clientId)
            {
                state = playerStates[index];
                return true;
            }
        }

        state = default;
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitGpsStateServerRpc(
        double latitude,
        double longitude,
        double altitude,
        float horizontalAccuracy,
        double deviceTimestamp,
        int gpsStatus,
        bool hasFix,
        ServerRpcParams serverRpcParams = default)
    {
        var senderClientId = serverRpcParams.Receive.SenderClientId;
        var nextState = new CoopPlayerGpsState(
            senderClientId,
            latitude,
            longitude,
            altitude,
            horizontalAccuracy,
            deviceTimestamp,
            gpsStatus,
            hasFix ? (byte)1 : (byte)0);

        for (var index = 0; index < playerStates.Count; index++)
        {
            if (playerStates[index].ClientId != senderClientId)
            {
                continue;
            }

            playerStates[index] = nextState;
            return;
        }

        playerStates.Add(nextState);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        for (var index = playerStates.Count - 1; index >= 0; index--)
        {
            if (playerStates[index].ClientId == clientId)
            {
                playerStates.RemoveAt(index);
            }
        }
    }

    private void HandlePlayerStatesChanged(NetworkListEvent<CoopPlayerGpsState> _)
    {
        StatesChanged?.Invoke();
    }
}

public struct CoopPlayerGpsState : INetworkSerializable, IEquatable<CoopPlayerGpsState>
{
    public ulong ClientId;
    public double Latitude;
    public double Longitude;
    public double Altitude;
    public float HorizontalAccuracy;
    public double DeviceTimestamp;
    public int GpsStatus;
    public byte HasFixFlag;

    public CoopPlayerGpsState(
        ulong clientId,
        double latitude,
        double longitude,
        double altitude,
        float horizontalAccuracy,
        double deviceTimestamp,
        int gpsStatus,
        byte hasFixFlag)
    {
        ClientId = clientId;
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
        HorizontalAccuracy = horizontalAccuracy;
        DeviceTimestamp = deviceTimestamp;
        GpsStatus = gpsStatus;
        HasFixFlag = hasFixFlag;
    }

    public bool HasFix => HasFixFlag != 0;

    public FixedString64Bytes ToSummaryString()
    {
        return new FixedString64Bytes($"Lat {Latitude:F6} | Lon {Longitude:F6}");
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Latitude);
        serializer.SerializeValue(ref Longitude);
        serializer.SerializeValue(ref Altitude);
        serializer.SerializeValue(ref HorizontalAccuracy);
        serializer.SerializeValue(ref DeviceTimestamp);
        serializer.SerializeValue(ref GpsStatus);
        serializer.SerializeValue(ref HasFixFlag);
    }

    public bool Equals(CoopPlayerGpsState other)
    {
        return ClientId == other.ClientId &&
               Latitude.Equals(other.Latitude) &&
               Longitude.Equals(other.Longitude) &&
               Altitude.Equals(other.Altitude) &&
               HorizontalAccuracy.Equals(other.HorizontalAccuracy) &&
               DeviceTimestamp.Equals(other.DeviceTimestamp) &&
               GpsStatus == other.GpsStatus &&
               HasFixFlag == other.HasFixFlag;
    }

    public override bool Equals(object obj)
    {
        return obj is CoopPlayerGpsState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClientId, Latitude, Longitude, Altitude, HorizontalAccuracy, DeviceTimestamp, GpsStatus, HasFixFlag);
    }
}

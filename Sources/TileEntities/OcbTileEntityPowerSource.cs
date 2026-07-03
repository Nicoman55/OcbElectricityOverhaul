using UnityEngine;

public class OcbTileEntityPowerSource : TileEntityPowerSource
{

    // ####################################################################
    // ####################################################################

    // Current mute state applied to the audio sources of the
    // block models "Activated" child (which holds the hum loop)
    private bool humMuted;

    // ####################################################################
    // ####################################################################

    public class OcbClientPowerData : ClientPowerData
    {
        public ushort MaxProduction;
        public ushort MaxGridProduction;
        public ushort LentConsumed;
        public ushort LentCharging;
        public ushort ChargingUsed;
        public ushort ChargingDemand;
        public ushort ConsumerUsed;
        public ushort ConsumerDemand;
        public ushort GridConsumerDemand;
        public ushort GridChargingDemand;
        public ushort GridConsumerUsed;
        public ushort GridChargingUsed;
        public ushort LentConsumerUsed;
        public ushort LentChargingUsed;
        public bool ChargeFromSolar;
        public bool ChargeFromGenerator;
        public bool ChargeFromBattery;
        public ushort LightLevel;
    }

    // ####################################################################
    // ####################################################################

    public OcbTileEntityPowerSource(Chunk _chunk) : base(_chunk) {}

    // ####################################################################
    // ####################################################################

    public new OcbClientPowerData ClientData => base.ClientData as OcbClientPowerData;

    // ####################################################################
    // ####################################################################

    private OcbTileEntityPowerSource(OcbTileEntityPowerSource _other, Chunk _chunk) : base(_chunk)
    {
        SetOwner(_other.GetOwner());
        PowerItem = _other.PowerItem;
    }

    // ####################################################################
    // ####################################################################

    public override TileEntity Clone() => new OcbTileEntityPowerSource(this, chunk);

    // ####################################################################
    // ####################################################################

    public override bool CanHaveParent(IPowered powered)
    {
        return PowerItemType == PowerItem.PowerItemTypes.BatteryBank ||
               PowerItemType == PowerItem.PowerItemTypes.SolarPanel ||
               PowerItemType == PowerItem.PowerItemTypes.Generator;
    }

    // ####################################################################
    // ####################################################################

    public override void write(PooledBinaryWriter _bw, StreamModeWrite _eStreamMode)
    {
        base.write(_bw, _eStreamMode);
        // Remove checks once everything works
        if (!(base.ClientData is OcbClientPowerData))
            Log.Error("Not of type ClientPowerDataOcb");
        // We assume this is valid if everything is ok
        var data = base.ClientData as OcbClientPowerData;
        switch (_eStreamMode)
        {
            case StreamModeWrite.Persistency:
                break;
            case StreamModeWrite.ToServer:
                _bw.Write(data.ChargeFromSolar);
                _bw.Write(data.ChargeFromGenerator);
                _bw.Write(data.ChargeFromBattery);
                break;
            default: // ToClient
                if (!(base.PowerItem is OcbPowerSource))
                    Log.Error("Not of type PowerSourceOcb");
                var source = base.PowerItem as OcbPowerSource;
                _bw.Write(source != null);
                if (source == null) break;
                // ToDo: check if we need em all (now 180 bytes)
                _bw.Write(source.MaxProduction);
                _bw.Write(source.MaxGridProduction);
                _bw.Write(source.ChargingUsed);
                _bw.Write(source.ChargingDemand);
                _bw.Write(source.ConsumerUsed);
                _bw.Write(source.ConsumerDemand);
                _bw.Write(source.LentConsumed);
                _bw.Write(source.LentCharging);
                _bw.Write(source.GridConsumerDemand);
                _bw.Write(source.GridChargingDemand);
                _bw.Write(source.GridConsumerUsed);
                _bw.Write(source.GridChargingUsed);
                _bw.Write(source.LentConsumerUsed);
                _bw.Write(source.LentChargingUsed);
                _bw.Write(source.ChargeFromSolar);
                _bw.Write(source.ChargeFromGenerator);
                _bw.Write(source.ChargeFromBattery);
                if (source is OcbPowerSolarPanel panel)
                    _bw.Write(panel.LightLevel);
                else _bw.Write((ushort)0);
                break;
        }

    }

    // ####################################################################
    // ####################################################################

    public override void read(PooledBinaryReader _br, StreamModeRead _eStreamMode)
    {
        base.read(_br, _eStreamMode);
        // Remove checks once everything works
        if (!(base.ClientData is OcbClientPowerData))
            Log.Error("Not of type ClientPowerDataOcb");
        // We assume this is valid if everything is ok
        var data = base.ClientData as OcbClientPowerData;
        switch (_eStreamMode)
        {
            case StreamModeRead.Persistency:
                break;
            case StreamModeRead.FromClient:
                data.ChargeFromSolar = _br.ReadBoolean();
                data.ChargeFromGenerator = _br.ReadBoolean();
                data.ChargeFromBattery = _br.ReadBoolean();
                if (!(base.PowerItem is OcbPowerSource))
                    Log.Error("Not of type PowerSourceOcb");
                var source = base.PowerItem as OcbPowerSource;
                source.ChargeFromSolar = data.ChargeFromSolar;
                source.ChargeFromGenerator = data.ChargeFromGenerator;
                source.ChargeFromBattery = data.ChargeFromBattery;
                break;
            default: // FromServer
                if (!_br.ReadBoolean()) break;
                // ToDo: check if we need em all (now 180 bytes)
                data.MaxProduction = _br.ReadUInt16();
                data.MaxGridProduction = _br.ReadUInt16();
                data.ChargingUsed = _br.ReadUInt16();
                data.ChargingDemand = _br.ReadUInt16();
                data.ConsumerUsed = _br.ReadUInt16();
                data.ConsumerDemand = _br.ReadUInt16();
                data.LentConsumed = _br.ReadUInt16();
                data.LentCharging = _br.ReadUInt16();
                data.GridConsumerDemand = _br.ReadUInt16();
                data.GridChargingDemand = _br.ReadUInt16();
                data.GridConsumerUsed = _br.ReadUInt16();
                data.GridChargingUsed = _br.ReadUInt16();
                data.LentConsumerUsed = _br.ReadUInt16();
                data.LentChargingUsed = _br.ReadUInt16();
                data.ChargeFromSolar = _br.ReadBoolean();
                data.ChargeFromGenerator = _br.ReadBoolean();
                data.ChargeFromBattery = _br.ReadBoolean();
                data.LightLevel = _br.ReadUInt16();
                break;
        }
    }

    // ####################################################################
    // ####################################################################

    // The battery bank hum loop is played by an AudioPlayer component
    // (7DTD's own audio wrapper, not a Unity AudioSource) that sits on
    // the block models "Activated" child GameObject. Vanilla toggles
    // that whole GameObject's active state together with the on/off
    // switch. Since we keep empty banks switched on (so they can be
    // recharged), we instead call AudioPlayer.StopAudio()/Play()
    // directly while the bank can neither produce power (all batteries
    // empty) nor is currently charging, without touching SetActive so
    // we don't interfere with vanilla's own on/off handling.
    public override void UpdateTick(World world)
    {
        base.UpdateTick(world);
        UpdateBatteryBankHum();
    }

    static readonly System.Reflection.MethodInfo AudioPlayerStopAudio =
        HarmonyLib.AccessTools.Method(typeof(AudioPlayer), "StopAudio");

    private void UpdateBatteryBankHum()
    {
        if (PowerItemType != PowerItem.PowerItemTypes.BatteryBank) return;
        bool isOn; ushort maxProduction; ushort chargingUsed;
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            if (!(PowerItem is OcbPowerBatteryBank bank)) return;
            isOn = bank.IsOn;
            maxProduction = bank.MaxProduction;
            chargingUsed = bank.ChargingUsed;
        }
        else
        {
            // On remote clients this data is only refreshed when the
            // tile entity is synced, so the hum may react delayed
            var data = ClientData;
            if (data == null) return;
            isOn = data.IsOn;
            maxProduction = data.MaxProduction;
            chargingUsed = data.ChargingUsed;
        }
        if (!isOn)
        {
            // Bank is fully off; vanilla already stops the hum by
            // disabling the "Activated" GameObject (SetActive(false)).
            // Leave that alone and just reset our tracked state so
            // the next power-on re-evaluates cleanly from scratch.
            humMuted = false;
            return;
        }
        bool mute = maxProduction <= 0 && chargingUsed <= 0;
        // Only touch the model on actual state changes
        if (mute == humMuted) return;
        // Dedicated servers have no transforms (guarded below)
        if (chunk == null) return;
        BlockEntityData bed = chunk.GetBlockEntity(ToWorldPos());
        if (bed == null || !bed.bHasTransform || bed.transform == null) return;
        Transform activated = bed.transform.Find("Activated");
        if (activated == null) return;
        AudioPlayer player = activated.GetComponent<AudioPlayer>();
        if (player == null) return;
        if (mute)
        {
            AudioPlayerStopAudio.Invoke(player, null);
        }
        else
        {
            player.Play();
        }
        humMuted = mute;
    }

    // ####################################################################
    // ####################################################################

}


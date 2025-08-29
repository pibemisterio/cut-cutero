using System;
using System.Collections.Generic;
namespace MMXOnline;

#region ▄▄★ TEMPLATE ★▄▄ 

public class X4Mushroom: Maverick {
    public static Weapon getWeapon() { return new Weapon(WeaponIds.FakeZeroGeneric, 150); }
    public float dashDist;
    public float baseSpeed = 50;
    public float accSpeed;
    public int lastDirX;
    // public Anim? exhaust;
    public float topSpeed = 200;
    public int shootNum = 0;

    // Ammo uses.
    public static int shootLv2Ammo = 3;
    public static int shootLv3Ammo = 4;

    // Main creation function.
    public X4Mushroom(
        Player player, Point pos, Point destPos, int xDir,
        ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
    ) : base(
        player, pos, destPos, xDir, netId, ownedByLocalPlayer
    ) {
        stateCooldowns = new() {
            { typeof(FakeZeroMeleeState), new(30) },
            { typeof(FakeZeroGroundPunchState), new(60) },
            { typeof(FakeZeroShootState), new(30, true) },
        };

        weapon = getWeapon();
        //	awardWeaponId = WeaponIds.Buster;
        //	weakWeaponId = WeaponIds.SpeedBurner;
        //	weakMaverickWeaponId = WeaponIds.FlameStag;
        //	canClimbWall = true;
        //	canClimb = true;

        netActorCreateId = NetActorCreateId.FakeZero;
        netOwner = player;
        if (sendRpc) {
            createActorRpc(player.id);
        }
        usesAmmo = true;
        canHealAmmo = true;
        ammo = 28;
        maxAmmo = 28;
        grayAmmoLevel = 3;
        barIndexes = (60, 49);
        //	gameMavs = GameMavs.X2;
        height = 36;
    }


    public override void update() {
        base.update();

        if (!ownedByLocalPlayer) return;

    }

    public override bool attackCtrl() {
        if (input.isHeld(Control.Shoot, player) && state is MRun) {
            changeState(new FakeZeroMeleeState());
            return true;
        }
        if (input.isHeld(Control.Shoot, player)) {
            changeState(new FakeZeroShootState(), false);
            return true;
        }
        if (input.isPressed(Control.Special1, player) && ammo >= shootLv3Ammo) {
            changeState(new FakeZeroMeleeState());
            return true;
        }
        if (input.isPressed(Control.Dash, player)) {
            changeState(new FakeZeroGroundPunchState());
            return true;
        }
        if (grounded) {
            if (input.isHeld(Control.Down, player) && state is not FakeZeroGuardState) {
                changeState(new FakeZeroGuardState());
                return true;
            }
        }
        return false;
    }


    public override float getRunSpeed() {
        float retSpeed = baseSpeed + accSpeed;
        if (retSpeed > Physics.WalkSpeed) {
            return retSpeed;
        }
        return Physics.WalkSpeed;
    }

    public override string getMaverickPrefix() {
        return "mav_x4mrm";
    }

    public override MaverickState[] strikerStates() {
        return [
            new FakeZeroShootState(2),
        ];
    }

    public override MaverickState[] aiAttackStates() {
        List<MaverickState> aiStates = [
            new FakeZeroShootState(1)
        ];

        return aiStates.ToArray();
    }

    public override void aiUpdate() {
        base.aiUpdate();


    }


    public MaverickState getShootState(bool isAI) {
        var mshoot = new MShoot((Point pos, int xDir) => {
            new FakeZeroBusterProj(
                pos, xDir, this, player.getNextActorNetId(), sendRpc: true
            );
        }, "busterX2");
        if (isAI) {
            mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.001f);
        }
        return mshoot;
    }

    public override void onDestroy() {
        base.onDestroy();

    }
}


#endregion
#region ▄▄▄■ STATES ■▄▄▄▄







#endregion

#region ▄▄▄▄⬤ PROJ ⬤▄▄▄▄







#endregion

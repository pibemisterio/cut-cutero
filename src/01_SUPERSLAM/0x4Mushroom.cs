using System;
using System.Collections.Generic;
namespace MMXOnline;

#region ▄★ MUSHROOM ★▄

public class X4Mushroom : Maverick {
    public static Weapon getWeapon() { return new Weapon(WeaponIds.FakeZeroGeneric, 150); }
    private float bodyCreation = 0;
    private const float BODY_CD = 0.7f;

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
        gravityModifier = 0.8f;
        usesAmmo = true;
        canHealAmmo = true;
        ammo = 28;
        maxAmmo = 28;
        grayAmmoLevel = 3;
        barIndexes = (60, 49);
        //	gameMavs = GameMavs.X2;
        height = 36;
    }


    #region ★ Update ━━━━━
    public override void update() {
        base.update();
        if (!ownedByLocalPlayer) return;
        bodyCreation += Global.spf;
        if (input.isHeld(Control.Shoot, player) && bodyCreation >= BODY_CD) {
            new MushroomBodyProj(pos, xDir, this, player, player.getNextActorNetId(), true);
            bodyCreation = 0;
        }
        if (state is MIdle or MRun or MLand) {
            if (input.isPressed(Control.Jump, player)) {
                changeState(new MushroomJump(0, isDoubleJump: false));
            }
            if (input.isHeld(Control.Down, player)) {
                changeState(new MushroomCroutch());
            }
            if (input.isHeld(Control.Left, player)) {
                xDir = -1;
                changeState(new MushroomRun(0));

            } else if (input.isHeld(Control.Right, player)) {
                xDir = 1;
                changeState(new MushroomRun(0));
            }

        }
    }
    #endregion
    #region ★ Atk Ctrl ━━━━━
    public override bool attackCtrl() {







        if (input.isPressed(Control.Special1, player)) {
            changeState(new FakeZeroMeleeState());
            return true;
        }




        return false;
    }
    #endregion


    public override float getRunSpeed() {

        return 69;
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






#region ■ Run ━━━━━━━
public class MushroomRun : MaverickState {
    public X4Mushroom minepe = null!;
    private float storedXSpeed;

    private bool isHoldingDirection;
    private int lastXDir;

    private const float SPEED_ACC = 180f;   //holding fowards
    private const float SPEED_DEACC = 230f; //not holding anything
    private const float SPEED_MAX = 200f;


    public MushroomRun(float storedXSpeed) : base("run") {
        this.storedXSpeed = storedXSpeed;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
    }

    public override void preUpdate() {
        base.preUpdate();
        lastXDir = maverick.xDir;
    }

    public override void update() {
        base.update();

        isHoldingDirection = false;

        //input move
        if (maverick.input.isHeld(Control.Left, maverick.player)) {
            maverick.xDir = -1;
            isHoldingDirection = true;
        } else if (maverick.input.isHeld(Control.Right, maverick.player)) {
            maverick.xDir = 1;
            isHoldingDirection = true;
        }

        maverick.move(new Point((int)(storedXSpeed * maverick.xDir), 0));

        //---------------------- Movement Calculation ------------------------------

        maverick.frameSpeed = Math.Max(1f, 1f * (storedXSpeed * 0.014f));  //animation speed
        //acc and deacc
        if (isHoldingDirection) {
            storedXSpeed = Math.Min(SPEED_MAX, storedXSpeed + (Global.spf * SPEED_ACC));
        } else {
            storedXSpeed = Math.Max(0, storedXSpeed - (Global.spf * SPEED_DEACC));
        }
        if (lastXDir != maverick.xDir && isHoldingDirection) {
            storedXSpeed = 0.25f;
        }

        if (!isHoldingDirection && storedXSpeed <= 30f) {
            maverick.changeState(new MIdle());
        }
        //Controller
        if (maverick.input.isPressed(Control.Jump, maverick.player)) {
            maverick.changeState(new MushroomJump(storedXSpeed, isDoubleJump: (maverick.grounded ? false : true)));
        }
        if (maverick.input.isPressed(Control.Down, maverick.player)) {
            maverick.changeState(new MushroomSpinDash(storedXSpeed * 1.2f));
        }
    }
}

#endregion

#region ■ Jump ━━━━━━━
public class MushroomJump : MaverickState {
    public X4Mushroom minepe = null!;
    MushrenzanProj? proj;
    bool isDoubleJump;
    float storedXSpeed;

    private bool stoppedHoldingJump;
    private bool isHoldingDirection;
    private int lastXDir;

    private const float SPEED_ACC = 180f;   //holding fowards
    private const float SPEED_DEACC = 230f; //not holding anything
    private const float SPEED_MAX = 200f;

    private const float JUMP_MOD = 1f;
    private const float JUMP_MOD2 = 1.2f;


    public MushroomJump(float storedXSpeed, bool isDoubleJump) : base(isDoubleJump ? "jump2" : "jump") {
        this.storedXSpeed = storedXSpeed;
        this.isDoubleJump = isDoubleJump;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        maverick.vel.y = -maverick.getJumpPower() * (isDoubleJump ? JUMP_MOD2 : JUMP_MOD);
        if (isDoubleJump) {
            proj = new MushrenzanProj(maverick.pos, maverick.xDir, maverick, player, player.getNextActorNetId(), true);
        }
    }
    public override void preUpdate() {
        base.preUpdate();
        lastXDir = maverick.xDir;
    }
    public override void update() {
        base.update();

        isHoldingDirection = false;

        //direction change
        if (maverick.input.isHeld(Control.Left, maverick.player)) {
            maverick.xDir = -1;
            isHoldingDirection = true;
        } else if (maverick.input.isHeld(Control.Right, maverick.player)) {
            maverick.xDir = 1;
            isHoldingDirection = true;
        }

        maverick.move(new Point((int)(storedXSpeed * maverick.xDir), 0));

        //----------------------Movement Calculation----------------------------
        if (isHoldingDirection) {
            // Accelerate when holding direction
            storedXSpeed = Math.Min(SPEED_MAX, storedXSpeed + (Global.spf * SPEED_ACC));
        } else {
            // Decelerate when NOT holding direction
            storedXSpeed = Math.Max(0, storedXSpeed - (Global.spf * SPEED_DEACC));
        }
        //without this, changing direction will do so at full storedXSpeed
        if (lastXDir != maverick.xDir && isHoldingDirection) {
            storedXSpeed = -storedXSpeed;
        }
        //---------------------------Jump Controller---------------------------
        //start falling when not holding Control.Jump
        if (stateTime >= 0.05f) {
            if (!player.input.isHeld(Control.Jump, player) || stateTime >= 0.4f)
                if (!stoppedHoldingJump) {
                    stoppedHoldingJump = true;
                    maverick.vel.y *= 0.4f; //smoother
                    if (!isDoubleJump) {
                        maverick.changeSpriteFromName("fall", true);
                    }
                }
        }

        //double jump
        if (stateTime >= 0.02f && maverick.input.isPressed(Control.Jump, maverick.player) && !isDoubleJump) {
            // hardcoded moving to the left then jumping to the rigth
            if (maverick.input.isHeld(Control.Right, maverick.player) && maverick.xDir == 1 && storedXSpeed < 0) {
                maverick.changeState(new MushroomJump(storedXSpeed * -1, isDoubleJump: true));
            }
            // hardcoded moving to the right then jumping to the left
            else if (maverick.input.isHeld(Control.Left, maverick.player) && maverick.xDir == -1 && storedXSpeed < 0) {
                maverick.changeState(new MushroomJump(storedXSpeed * -1, isDoubleJump: true));
            } else {
                // if not jump normally.... papu wtF
                maverick.changeState(new MushroomJump(storedXSpeed, isDoubleJump: true));
            }
        }

        //end when grounded
        if (maverick.grounded && stateTime >= 0.1f) {
            //hardcoded canceling momentum if holding the opposite direction
            if (maverick.input.isHeld(Control.Right, maverick.player) && maverick.xDir == 1 && storedXSpeed < 0) {
                maverick.changeState(new MushroomRun(0));
            }
            //same here
            else if (maverick.input.isHeld(Control.Left, maverick.player) && maverick.xDir == -1 && storedXSpeed < 0) {
                maverick.changeState(new MushroomRun(0));
            } else {
                // if not jump normally.... papu wtF santos skibidis hardcoderos
                maverick.changeState(new MushroomRun(storedXSpeed));
            }

        }
    }
}
#endregion

#region ■ Croutch ━━━━━
public class MushroomCroutch : MaverickState { //aka spindash start
    public X4Mushroom minepe = null!;
    private bool startedSpindashing;
    private float revLevel = 0;
    private float timeSinceLastRev = 1f;

    private const float MIN_LEVEL = 0;
    private const float MAX_LEVEL = 3;

    private readonly float[] levelFrameSpeeds = { 2f, 5f, 8f, 12f };
    private readonly float[] levelSpeeds = { 250f, 350f, 450f, 550f };

    private float levelSpeed = 0f;

    public MushroomCroutch() : base("croutch") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        maverick.stopMoving();
    }

    public override void update() {
        base.update();
        levelSpeed = levelSpeeds[(int)revLevel];
        //------------------------------Croutch Handling------------------------
        // Initiate SpinDash/Croutch
        if (!input.isHeld(Control.Down, player)) {
            if (!startedSpindashing) {
                maverick.changeState(new MIdle());
            } else {
                maverick.changeState(new MushroomSpinDash(levelSpeed));
            }
        }
        //Initiate Revvin'
        if (input.isPressed(Control.Dash, player) && !startedSpindashing) {
            startedSpindashing = true;
            revLevel = 0;
            maverick.changeSpriteFromName("3dash_spin", true);
        }

        //------------------------------Factor Handling------------------------

        //Decrease factor over time 
        if (startedSpindashing) {
            maverick.frameSpeed = levelFrameSpeeds[(int)revLevel]; //anim speed
            timeSinceLastRev -= Global.spf * 2f;   // 3.5 equals decrease time mult
        }
        if (timeSinceLastRev <= 0) {
            timeSinceLastRev = 1f;
            revLevel = Math.Max(MIN_LEVEL, revLevel - 1);
        }
        //Increase Factor by Control.Dash
        if (input.isPressed(Control.Dash, player) && startedSpindashing) {
            revLevel = Math.Min(MAX_LEVEL, revLevel + 1);
            timeSinceLastRev = 1f;

            new Anim(maverick.pos.addxy(5 * -maverick.xDir, 0), "dash_sparks",
            maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
        }
    }
}
#endregion


#region ■ Spin Dash ━━━━
public class MushroomSpinDash : MaverickState { //Copypasted from Run, but removing a lot of
    public X4Mushroom minepe = null!;           //input holding mechanics not present in sanic
    private float storedXSpeed;

    private bool isHoldingDirection;
    private bool stoppedHoldingJump;
    private bool shouldDeaccMore;
    private bool oldStateWasRun;
    private int lastXDir;

    private const float SPEED_DEACC = 160f; //not holding anything
    private const float SPEED_DEACC_HOLDING = 420f;  //holding opposite direction to stop
    private const float SPEED_MAX = 300f;


    public MushroomSpinDash(float storedXSpeed) : base("3dash_spin") {
        this.storedXSpeed = storedXSpeed;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        if (oldState is MushroomRun) {
            oldStateWasRun = true;
        }
    }

    public override void preUpdate() {
        base.preUpdate();
        lastXDir = maverick.xDir;
    }
    float jumpTime = 0;
    public override void update() {
        base.update();
        //mimic stateTime for jump handling
        if (!maverick.grounded) {
            jumpTime += Global.spf;
        } else {
            jumpTime = 0;
        }

        maverick.move(new Point((int)(storedXSpeed * maverick.xDir), 0));

        if (maverick.xDir == -1 && maverick.input.isHeld(Control.Right, maverick.player) ||
            maverick.xDir == 1 && maverick.input.isHeld(Control.Left, maverick.player)) {
            shouldDeaccMore = true;
        }
        //flip when WALL hit
        var hitWall = Global.level.checkTerrainCollisionOnce(maverick, 1 * maverick.xDir, -2);
        if (hitWall?.isSideWallHit() == true) {
            maverick.xDir *= -1;
            float jumpXSpeed = Math.Abs(storedXSpeed) * 1;
            maverick.vel.y = -maverick.getJumpPower() * Math.Abs(storedXSpeed) * 0.002f;

        }
        //----------------------Movement Calculation------------------------------------
        //animation speed * math
        maverick.frameSpeed = Math.Max(1f, 1f * (storedXSpeed * 0.035f));
        storedXSpeed = Math.Max(0, storedXSpeed - (Global.spf * (shouldDeaccMore ? SPEED_DEACC_HOLDING : SPEED_DEACC)));

        if (maverick.grounded) {
            if (storedXSpeed <= 15f) {
                maverick.changeState(new MIdle());
            }
            if (storedXSpeed <= (shouldDeaccMore ? 60 : 240) && !oldStateWasRun) {
                if (maverick.input.isHeld(Control.Left, maverick.player) || maverick.input.isHeld(Control.Right, maverick.player)) {
                    maverick.changeState(new MushroomRun(storedXSpeed));
                }
            }
        }
        //----------------------Jump Controller------------------------------------
        if (maverick.input.isPressed(Control.Jump, maverick.player) && maverick.grounded) {
            maverick.vel.y = -maverick.getJumpPower() * 1.3f;
            jumpTime = 0;
            stoppedHoldingJump = false;
        }
        if (jumpTime >= 0.06f) { //buffer
            if (!player.input.isHeld(Control.Jump, player) || jumpTime >= 0.25f)
                if (!stoppedHoldingJump) {
                    stoppedHoldingJump = true;
                    maverick.vel.y *= 0.4f; //smoother
                    jumpTime = 0;
                }
        }

    }
}
#endregion

#region ■ Flinch ━━━━━━
public class MushroomFlinch : MaverickState {
    public X4Mushroom minepe = null!;


    public MushroomFlinch() : base("1atk") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        maverick.stopMoving();
    }

    public override void update() {
        base.update();

        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
    }
}
#endregion

#region ■ Headbutt ━━━━
public class MusrhoomHeadbutt : MaverickState {
    public X4Mushroom minepe = null!;
    //bool startedGrounded;

    public MusrhoomHeadbutt() : base("1atk") {
        //public MavState(bool startedGrounded) : base(startedGrounded ? "1atk" : "1atk_air") {
        //this.startedGrounded = startedGrounded;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        maverick.stopMoving();
    }

    public override void update() {
        base.update();

        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
    }
}
#endregion

#region ■ Giga ━━━━━━━
public class MushroomGiga : MaverickState {
    public X4Mushroom minepe = null!;
    //bool startedGrounded;

    public MushroomGiga() : base("1atk") {
        //public MavState(bool startedGrounded) : base(startedGrounded ? "1atk" : "1atk_air") {
        //this.startedGrounded = startedGrounded;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        maverick.stopMoving();
    }

    public override void update() {
        base.update();

        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
    }
}
#endregion
#endregion

#region ▄▄▄▄⬤ PROJ ⬤▄▄▄▄







#region ⬤ Mushrenzan ━━━
public class MushrenzanProj : Projectile {
    public X4Mushroom minepe = null!;
    public MushrenzanProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4mrm_jump2_proj", netId, player
    ) {
        minepe = owner as X4Mushroom ?? throw new NullReferenceException();
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = Point.zero;
        maxTime = 0.3f;
        destroyOnHit = false;
        destroyOnHitWall = false;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new MushroomBodyProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
        if (isAnimOver()) destroySelf();
    }
    public override void postUpdate() {
        base.postUpdate();
        if (owner?.character != null) {
            changePos(owner.character.pos);
        }
    }
    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Mushroom Body ━
public class MushroomBodyProj : Projectile {
    private bool hasLandedOnce;

    public MushroomBodyProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4mrm_1atk_body_start", netId, player
    ) {
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        damager.damage = 2;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = new Point((65 + new Random().Next(-25, 26)) * xDir, -200 + new Random().Next(-30, 31));
        maxTime = 3.2f;
        //----------------------------//       
        useGravity = true;
        gravityModifier = 0.5f;
        destroyOnHit = true;
        destroyOnHitWall = false;



        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new MushroomBodyProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
        globalCollider = new Collider(new Rect(0, 0, 25, 35).getPoints(),
        false, this, false, false, HitboxFlag.HitAndHurt, Point.zero); //isTrigger false first bool

        if (vel.y >= 10) {
            changeSprite("mav_x4mrm_fall", false);
        }
        if (this.grounded && !hasLandedOnce) {
            hasLandedOnce = true;
            vel = Point.zero;
            changeSprite("mav_x4mrm_land", false);
        }
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Fake Mushroom ━
public class TypedProj : Projectile {
    //public int type;
    public TypedProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        //this.type = type;
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = new Point(300 * xDir, 0);
        maxTime = 1.2f;
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";

        switch (type) {
            case 0:
                break;
            case 1:
                break;
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new TypedProj(
            args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion
#region ⬤ Poison Cloud ━━━
public class NeutralProj : Projectile {
    public NeutralProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = new Point(300 * xDir, 0);
        maxTime = 1.2f;
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new NeutralProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion
#region ⬤ Bouncy Giga ━━━


#endregion



#endregion

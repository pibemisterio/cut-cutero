namespace MMXOnline;

using System;
using System.Collections.Generic;

//as sparkmandril
#region ▄▄★ DRAGOON ★▄▄ 

public class X4Dragoon : Maverick {
    public Weapon meleeWeapon;

    private float satsuTimer = 0;
    private int satsuStep = 0;
    private const float SatsuTimeWindow = 0.15f; // time allowed between inputs

    private bool specialedInAir = false; //prevent flying with special

    public SatsuWeapon satsuWeapon = new();

    public static Weapon getWeapon() { return new Weapon(WeaponIds.X4Dragoon, 146); }
    public X4Dragoon(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
        base(player, pos, destPos, xDir, netId, ownedByLocalPlayer) {
        stateCooldowns = new() {
            //   { typeof(SparkMPunchState), new(60, true, true) },
            // { typeof(SparkMDashPunchState), new(45, false, true) },
            //{ typeof(MShoot), new(2 * 60, true, true) }
        };
        //stateCooldowns.Add(typeof(MaverickAbbrSpecialState), new MaverickStateCooldown(false, true, 0.75f));

        weapon = new Weapon(WeaponIds.DragoonGeneric, 0);
        meleeWeapon = new Weapon(WeaponIds.DragoonGeneric, 0);
        netActorCreateId = NetActorCreateId.X4Dragoon;
        netOwner = player;

        if (sendRpc) {
            createActorRpc(player.id);
        }
    }

    public override void update() {
        base.update();


        if (satsuStep > 0) {
            satsuTimer -= Global.spf;
            if (satsuTimer <= 0) {
                satsuStep = 0;
            }
        }

        switch (satsuStep) {
            case 0:
                if (player.input.isPressed(Control.Shoot, player)) {
                    satsuStep = 1;
                    satsuTimer = SatsuTimeWindow;
                }
                break;
            case 1:
                if (player.input.isPressed(Control.Shoot, player)) {
                    satsuStep = 2;
                    satsuTimer = SatsuTimeWindow;
                }
                break;
            case 2:
                if (player.input.isPressed(Control.Right, player) || player.input.isPressed(Control.Left, player)) { //todo allow xdir flip
                    satsuStep = 3;
                    satsuTimer = SatsuTimeWindow;
                }
                break;
            case 3:
                if (player.input.isPressed(Control.Dash, player)) {
                    satsuStep = 4;
                    satsuTimer = SatsuTimeWindow;
                }
                break;
            case 4:
                if (player.input.isPressed(Control.Special1, player) && player.currency >= 0) {
                    changeState(new DragoonSatsuDash());
                    satsuStep = 0;
                }
                break;
        }

        if (grounded) {
            specialedInAir = false;
        }
        if (aiBehavior == MaverickAIBehavior.Control) {
            if (state is MIdle or MRun or MLand) {
                if (player.input.isHeld(Control.Down, player) && shootPressed()) {
                    changeState(new DragoonHadoken(startedGrounded: this.grounded, startedHoldingDown: true));

                } else if (shootPressed()) {
                    changeState(new DragoonHadoken(startedGrounded: this.grounded, startedHoldingDown: false));

                } else if (input.isPressed(Control.Special1, player)) {
                    changeState(new DragoonFireBreathStart(startedGrounded: this.grounded));

                } else if (input.isPressed(Control.Dash, player)) {
                    changeState(new DragoonShoryuken());

                } else if (input.isPressed(Control.Special2, player)) {
                    if (ammo >= 32)
                        changeState(new DragoonGigaAttack(startedGrounded: this.grounded));
                }
            } else if (state is MJump || state is MFall) {
                if (player.input.isHeld(Control.Down, player) && shootPressed()) {
                    changeState(new DragoonHadoken(startedGrounded: this.grounded, startedHoldingDown: true));

                } else if (shootPressed()) {
                    changeState(new DragoonHadoken(startedGrounded: this.grounded, startedHoldingDown: false));

                } else if (input.isPressed(Control.Special1, player) && !specialedInAir) {
                    specialedInAir = true;
                    changeState(new DragoonFireBreathStart(startedGrounded: this.grounded));

                } else if (input.isPressed(Control.Dash, player)) {
                    changeState(new DragoonDivekick());

                } else if (input.isPressed(Control.Special2, player)) {
                    if (ammo >= 32)
                        changeState(new DragoonGigaAttack(startedGrounded: this.grounded));
                }
            }
        }
    }
    public override float getRunSpeed() {
        return 155;
    }
    public override string getMaverickPrefix() {
        return "mav_x4dgn";
    }

    public override MaverickState getRandomAttackState() {
        return aiAttackStates().GetRandomItem();
    }

    public override MaverickState[] aiAttackStates() {
        return new MaverickState[]
        {
/*          new MaverickAbbrShootState(),
            new MaverickAbbrMeleeState(),
            new MaverickAbbrSpecialState(), */
        };
    }

    public MaverickState getShootState(bool isAI) {
        var mshoot = new MShoot((Point pos, int xDir) => {
            playSound("???", sendRpc: true);
            // new FakeZeroBusterProj(weapon, pos, xDir, player, player.getNextActorNetId(), rpc: true);
        }, null);
        if (isAI) {
            mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.75f);
        }
        return mshoot;
    }

    public class SatsuWeapon : Weapon {
        public SatsuWeapon() {
            index = (int)WeaponIds.DragoonGeneric;
            killFeedIndex = 94;
        }
    }

    public enum MeleeIds {
        None = -1,
        SatsuId,

    }

    // This can run on both owners and non-owners. So data used must be in sync.
    public override int getHitboxMeleeId(Collider hitbox) {
        return (int)(sprite.name switch {
            "mav_x4dgn_5satsu_start" => MeleeIds.SatsuId,
            _ => MeleeIds.None
        });
    }

    // This can be called from a RPC, so make sure there is no character conditionals here.
    public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
        return (MeleeIds)id switch {

            MeleeIds.SatsuId => new GenericMeleeProj(
                satsuWeapon, pos, ProjIds.SatsuId, player,
                0, 0, 45, addToLevel: addToLevel
            ),
            _ => null
        };
    }
}
#endregion
#region ▄▄▄■ STATES ■▄▄▄▄







#region ■ Hdk state ━━━━
public class DragoonHadoken : MaverickState {
    public X4Dragoon overratedlizard = null!;
    private bool hasCharged = true;
    private bool hasShot = false;
    bool startedGrounded;
    bool startedHoldingDown;

    public DragoonHadoken(bool startedGrounded, bool startedHoldingDown) :
    base(startedGrounded ? startedHoldingDown ? "1atk_down" : "1atk" : startedHoldingDown ? "1atk_down_air" : "1atk_air") {
        //kinda fucked up but has to be like this for the air transitions // you son of a bitch
        this.startedGrounded = startedGrounded;
        this.startedHoldingDown = startedHoldingDown;
        landSprite = startedHoldingDown ? "1atk_down" : "1atk";
        airSprite = startedHoldingDown ? "1atk_down_air" : "1atk_air";
        canJump = true;
    }
    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        overratedlizard = maverick as X4Dragoon ?? throw new NullReferenceException();
    }

    public override void update() {
        base.update();

        //bigshot
        if (maverick.frameIndex >= 5 && maverick.frameIndex <= 16 && !player.input.isHeld(Control.Shoot, player)) {
            maverick.frameIndex = 17;
            hasCharged = true;
            //smallshot
        } else if (maverick.frameIndex <= 4 && !player.input.isHeld(Control.Shoot, player)) {
            maverick.frameIndex = 17;
            hasCharged = false;
        }
        //if fires smallshot, less endlag
        if (maverick.frameIndex >= 21 && !hasCharged) {
            maverick.frameSpeed = 1.5f;
        }
        //fire
        if (maverick.frameIndex >= 17 && !hasShot) {
            hasShot = true;
            fireHadokenProj();
        }
        //stopMoving() && gravMod = 0f + this; makes charged linger on air
        if (!maverick.grounded && hasShot && hasCharged) {
            maverick.gravityModifier += Global.spf * 0.7f;
        }
        //input airmove
        if (!maverick.grounded && !(hasShot && hasCharged)) {
            Point moveAmount = new Point(player.input.getInputDir(this.player).x * 120, 0);
            maverick.move(moveAmount);
        }
        //don't jump after big shoot
        if (hasShot && hasCharged) {
            canJump = false;
        }
        //bnnuy hop
        if (maverick.isAnimOver() || maverick.grounded && !startedGrounded && !hasCharged && hasShot) {
            maverick.changeState(new MIdle()); //todo "land"
        }
    }
    private void fireHadokenProj() {
        Point? shootPos = maverick.getFirstPOI() ?? maverick.getCenterPos();
        // update either down hadoken or straight hadoken // 1 down 0 straight
        int updatedType = maverick.sprite.name == "mav_x4dgn_1atk_down_air" ? 1 : 0;

        if (hasCharged) {
            maverick.playSound("x4dgn_haduken", sendRpc: true); //voice 
                                                                //  maverick.shakeCameraSmall();
            maverick.stopMoving();
            maverick.gravityModifier = 0f;
            maverick.xPushVel = -125 * maverick.xDir;
            maverick.yPushVel = maverick.grounded ? 0 : -150;
            //maverick.pushEffect(0, 0);

            new DragoonHadokenStrongProj(
            shootPos.Value, maverick.xDir, updatedType, overratedlizard,
            player, player.getNextActorNetId(), rpc: true
            );
            new Anim(shootPos.Value, "mav_x4dgn_3dash_crash_fx",
            maverick.xDir, player.getNextActorNetId(), true, sendRpc: true
            ) {
                vel = new Point(-200 * maverick.xDir, updatedType == 1 ? -200 : 0),
                byteAngle = (updatedType == 1 ? 96 : 64) * maverick.xDir,
                ttl = 0.09f
            };
        } else {
            new DragoonHadokenWeakProj(
            shootPos.Value, maverick.xDir, updatedType, overratedlizard,
            player, player.getNextActorNetId(), rpc: true);
        }
    }
    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.gravityModifier = 1f;
    }
}
#endregion

#region ■ Firebreath start ━
public class DragoonFireBreathStart : MaverickState { //todo specialed inair on baseclass
    public X4Dragoon overratedlizard = null!;
    bool startedGrounded;

    public DragoonFireBreathStart(bool startedGrounded) : base(startedGrounded ? "2spcl" : "2spcl_air") {
        this.startedGrounded = startedGrounded;
        landSprite = "2spcl";
        airSprite = "2spcl_air";
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        overratedlizard = maverick as X4Dragoon ?? throw new NullReferenceException();
        //subtly move to the opposite dir
        if (!startedGrounded) {
            maverick.stopMoving();
            maverick.gravityModifier = 0f;
            maverick.xPushVel = -320 * maverick.xDir;
            maverick.yPushVel = -220;
        }
    }

    public override void update() {
        base.update();
        maverick.gravityModifier += Global.spf * 1.8f;
        if (maverick.isAnimOver()) {
            maverick.changeState(new DragoonFireBreathLoop());
        }
        //end if not holding special
        if (maverick.frameIndex > 1 && !player.input.isHeld(Control.Special1, player)) {
            maverick.changeToIdleOrFall();
            return;
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.gravityModifier = 1f;
    }
}
#endregion

#region ■ Firebreath loop ━
public class DragoonFireBreathLoop : MaverickState {
    public X4Dragoon overratedlizard = null!;
    private bool hasShot;
    private bool hasEnded;
    private int breathCount;
    private const int MAX_BREATHS = 24;

    public DragoonFireBreathLoop() : base("2spcl_loop") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        overratedlizard = maverick as X4Dragoon ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.shakeCamera(sendRpc: true);
        maverick.playSound("x4dgn_special", sendRpc: true); //voice 
    }

    private int lastSoundLoopCount = -1; // play sound after 2 shoots
    public override void update() {
        base.update();

        //fire
        if (maverick.frameIndex == 3 && !hasShot) {
            hasShot = true;
            breathCount++;
            fireBreathProj();
        }
        //usual transition logic inside the same json & reset fire
        if (maverick.frameIndex >= 5 && breathCount < MAX_BREATHS) {
            hasShot = false;
            maverick.frameIndex = 3;
        }
        //after frame 5, the json continues the endlag anim
        if (breathCount >= MAX_BREATHS && !hasEnded) {
            maverick.frameIndex = 5;
            hasEnded = true;
        }
        if (maverick.isAnimOver()) {
            maverick.changeToIdleOrFall();
        }
    }
    private void fireBreathProj() {
        Point? shootPos = maverick.getFirstPOI() ?? maverick.getCenterPos();
        new DragoonBreathProj(
            shootPos.Value, maverick.xDir, overratedlizard,
            player, player.getNextActorNetId(), rpc: true
        );
        new Anim(shootPos.Value, "mav_x4dgn_3dash_crash_fx",
        maverick.xDir, player.getNextActorNetId(), true, sendRpc: true
        ) {
            vel = new Point(160 * maverick.xDir, 60), byteAngle = 64 * maverick.xDir,
            xScale = 0.5f, yScale = 0.5f, ttl = 0.09f
        };
        if (maverick.loopCount % 2 == 0 && maverick.loopCount != lastSoundLoopCount) {
            maverick.playSound("x4dgn_fire_loop", sendRpc: true);
            lastSoundLoopCount = maverick.loopCount;
        }
    }
}
#endregion

#region ■ Shoryuken ━━━━
public class DragoonShoryuken : MaverickState {
    public X4Dragoon overratedlizard = null!;
    DragoonShoryukenProj? proj0;
    DragoonShoryukenProj? proj1;
    private float targetSpeed = 0;
    private float initialJumpPower;
    private bool hasStartedJump = false;

    public DragoonShoryuken() : base("3dash") {
    }
    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        overratedlizard = maverick as X4Dragoon ?? throw new NullReferenceException();
    }
    float naturalDashMove = 100;
    public override void update() {
        base.update();

        //jumps, frame <= 6 is the startup   //a lot of fumbled finetunning, don't overthink when looking
        if (maverick.frameIndex == 9 && !hasStartedJump) {
            hasStartedJump = true;
            maverick.vel.y = -maverick.getJumpPower() * 0.9f;
            setShoryukenProjPushAndEffect();
        }
        // input move (move fowards a bit always + an input factor)
        if (maverick.frameIndex >= 10) {
            naturalDashMove -= Global.spf * 180f;
            Point moveAmount = new Point(naturalDashMove * maverick.xDir + (player.input.getInputDir(this.player).x * 120), 0);
            maverick.move(moveAmount);
        }
        //finetune the ending, otherwise has no weight
        if (stateTime > 0.46f) {
            maverick.gravityModifier -= Global.spf * 3f;
        }
        // state transition logic
        if (stateTime > 0.85f || !player.input.isHeld(Control.Dash, player) && stateTime > 0.40f) {
            maverick.changeState(new MFall());
            return;
        }
        //simple wallcrash end
        var hitWall = Global.level.checkTerrainCollisionOnce(maverick, 0, -15);
        if (hitWall?.gameObject is Wall wall && !wall.topWall) {
            maverick.playSound("crash", sendRpc: true);
            maverick.shakeCamera(sendRpc: true);
            new Anim(maverick.pos.addxy(maverick.xDir * 10, -30), "mav_x4dgn_3dash_crash_fx", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true) {
                yDir = -1
            };
            maverick.changeState(new MFall());
            return;
        }
    }
    private void setShoryukenProjPushAndEffect() {
        maverick.playSound("x4dgn_shoryuken", sendRpc: true); //voice 
        maverick.playSound("x4fre_shoot", sendRpc: true);
        //these make the move feel stronger
        maverick.xPushVel = 120 * maverick.xDir;
        maverick.yPushVel = -240;

        proj0 = new DragoonShoryukenProj(maverick.pos.addxy(22 * maverick.xDir, -75), maverick.xDir, maverick, player, player.getNextActorNetId(), true);
        proj1 = new DragoonShoryukenProj(maverick.pos.addxy(22 * maverick.xDir, -45), maverick.xDir, maverick, player, player.getNextActorNetId(), true);
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.gravityModifier = 1f;
        proj0?.destroySelf();
        proj1?.destroySelf();
    }
}
#endregion

#region ■ Divekick ━━━━━
public class DragoonDivekick : MaverickState {
    public X4Dragoon overratedlizard = null!;
    DragoonDivekickProj? proj;
    private bool hasKicked;


    public DragoonDivekick() : base("3dash_air") {
        useGravity = false;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        overratedlizard = maverick as X4Dragoon ?? throw new NullReferenceException();
        maverick.stopMoving();
    }

    public override void update() {
        base.update();
        // if less than 9 just move up a bit, then execute the kick
        if (maverick.frameIndex <= 9) {
            maverick.move(new Point(-50 * maverick.xDir, -50));
            return;
        }
        //kick
        maverick.move(new Point(maverick.xDir * 700, 600));

        if (!hasKicked) {
            hasKicked = true;
            maverick.playSound("x4dgn_dash_air", sendRpc: true); //voice 
            proj = new DragoonDivekickProj(maverick.pos.addxy(55 * maverick.xDir, 18), maverick.xDir, maverick, player, player.getNextActorNetId(), true);
        }
        //simple ground crash end
        if (maverick.grounded) {
            maverick.changeState(new DragoonDivekickRecoil());
            playCrashFx(crashedGround: true);
        }
        //wallcrash hit flip
        var hitWall = Global.level.checkTerrainCollisionOnce(maverick, maverick.vel.x * Global.spf * 0.5f, -5);
        if (hitWall?.isSideWallHit() == true) {
            //flip
            maverick.xDir *= -1;
            maverick.move(new Point(-maverick.vel.x * 0.3f * Global.spf, 0));
            proj.xDir *= -1;
            //proj gets all fumbled up after flip so we fix it here
            proj.changePos(maverick.pos.addxy(50 * maverick.xDir, -2));
            playCrashFx(crashedGround: false);
        }
    }

    private void playCrashFx(bool crashedGround) {
        maverick.playSound("crash", sendRpc: true);
        maverick.shakeCamera(sendRpc: true);
        //bool just to flip the anim
        if (crashedGround) {
            new Anim(maverick.pos.addxy(maverick.xDir * 10, 0), "mav_x4dgn_3dash_crash_fx",
            maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
        } else {
            new Anim(maverick.pos.addxy(maverick.xDir * 10, 0), "mav_x4dgn_3dash_crash_fx",
            maverick.xDir, player.getNextActorNetId(), true, sendRpc: true) {
                byteAngle = 64 * -maverick.xDir
            };
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        proj?.destroySelf();
    }
}
#endregion

#region ■ Recoil ━━━━━━
public class DragoonDivekickRecoil : MaverickState {
    public X4Dragoon overratedlizard = null!;
    private float moveSpeed = 150;
    private float targetSpeed = 0;
    public DragoonDivekickRecoil() : base("3dash_recoil") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        overratedlizard = maverick as X4Dragoon ?? throw new NullReferenceException();
        maverick.playSound("x4dgn_land", sendRpc: true);
    }

    public override void update() {
        base.update();
        //move made the old way, just leave it like this // todo shut the fuck up fix it
        var move = new Point((int)(moveSpeed * -maverick.xDir), 0);
        maverick.move(move);

        float jumpMod = 0.25f;
        moveSpeed = Helpers.lerp(moveSpeed, targetSpeed, Global.spf * 3.3f);

        if (stateTime <= 0.1f) {
            maverick.vel.y = -maverick.getJumpPower() * jumpMod * 1f;
        }
        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }

        if (stateTime >= 0.2f && maverick.grounded) {
            maverick.changeState(new MIdle());
        }
    }
}
#endregion

#region ■ Giga State ━━━━
public class DragoonGigaAttack : MaverickState {
    public X4Dragoon overratedlizard = null!;
    DragoonGigaAuraProj? aura;
    DragoonMeteorGenerator? gen;
    DragoonUpwardsMeteor? upMeteor;

    bool startedGrounded;
    bool hasShot;
    bool hasSpawnAura;


    public DragoonGigaAttack(bool startedGrounded) : base(startedGrounded ? "4giga" : "4giga_air") {
        this.startedGrounded = startedGrounded;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        overratedlizard = maverick as X4Dragoon ?? throw new NullReferenceException();
        maverick.stopMoving();

        gen = new DragoonMeteorGenerator(
       maverick.pos, maverick.xDir, overratedlizard,
       player, player.getNextActorNetId(), rpc: true
   );

    }

    public override void update() {
        base.update();

        //float a bit on air
        if (!startedGrounded) {
            maverick.move(new Point(0, -10));
            maverick.gravityModifier = 0;
        }
        //fire
        Point? shootPos = maverick.getFirstPOI() ?? maverick.getCenterPos();
        if ((maverick.frameIndex == 4) && !hasShot) {
            hasShot = true;
            maverick.shakeCamera(sendRpc: true);
            maverick.playSound("x4dgn_fire_loop", sendRpc: true);

            if (gen != null) {
                upMeteor = new DragoonUpwardsMeteor(
                   shootPos.Value, maverick.xDir, overratedlizard,
                   player, player.getNextActorNetId(), rpc: true
                    );
                upMeteor.gen = this.gen;
            }
            //auraspawn nested inside 
            if (!hasSpawnAura) {
                hasSpawnAura = true;
                aura = new DragoonGigaAuraProj(
                    maverick.pos, maverick.xDir, startedGrounded ? 0 : 1, overratedlizard,
                    player, player.getNextActorNetId(), rpc: true);
            }
        }
        //reset fire
        if (maverick.frameIndex == 5) {
            hasShot = false;
        }
        //end
        if (maverick.loopCount >= 16) {
            maverick.changeState(new MIdle("4giga_end")); // todo oo son of a bitch kill this json
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        aura?.destroySelf();
        maverick.gravityModifier = 1f;
    }

}
#endregion

#region ■ Satsu Dash ━━━━
public class DragoonSatsuDash : MaverickState {
    public X4Dragoon overratedlizard = null!;
    public Anim? firstSpark;
    public Anim? secondSpark;

    bool hasSparked;
    bool hasSparked2;

    public DragoonSatsuDash() : base("5satsu_start") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        overratedlizard = maverick as X4Dragoon ?? throw new NullReferenceException();
        maverick.playSound("x4dgn_ragin", sendRpc: true); //voice 
        maverick.stopMoving();
        maverick.frameSpeed = 0;
        maverick.gravityModifier = 0;

        new Anim(maverick.pos.addxy(0 * -maverick.xDir, 0), "mav_x4dgn_5satsu_start_Fx",
        maverick.xDir, player.getNextActorNetId(), true, sendRpc: true
        );
    }
    float currentSpeed = 500;
    float partTime;
    public override void update() {
        base.update();

        secondSpark?.incPos(maverick.deltaPos);

        Point? grabPos = maverick.getFirstPOI() ?? maverick.getCenterPos();
        //play the first grab indicator after "mav_x4dgn_5satsu_start_Fx" ends
        if (stateTime == 0.7f && !hasSparked) {
            hasSparked = true;
            firstSpark = new Anim(
                grabPos.Value,
                "fx_grab_start", maverick.xDir, player.getNextActorNetId(),
                true, sendRpc: true
            );
        }
        //move fowards
        if (stateTime >= 0.8f) {
            maverick.move(new Point((int)(currentSpeed * maverick.xDir), 0));
            currentSpeed = Helpers.lerp(currentSpeed, 0, Global.spf * 2f);
            //jump to the frame index with the hitbox,
            if (!hasSparked2) {
                maverick.frameIndex = 1;
                maverick.playSound("x4dgn_ragin_dash", sendRpc: true);
                hasSparked2 = true;
                secondSpark = new Anim(
                grabPos.Value.addxy(8 * maverick.xDir, 0),
                "fx_grab_indicator", maverick.xDir, player.getNextActorNetId(),
                 true, sendRpc: true
                  );
            }
        }
        //afterimage part
        partTime += Global.spf;
        if (partTime >= 0.01f) {
            partTime = 0;
            new Anim(maverick.pos.addxy(0 * maverick.xDir, 0),
                "mav_x4dgn_5satsu_image", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true) {
                zIndex = maverick.zIndex - 30
            };
        }
        if (stateTime >= 2f) {
            maverick.changeToIdleRunOrFall();
        }
    }
    public override bool trySetGrabVictim(Character grabbed) {
        maverick.changeState(new DragoonExecutingSatsu(grabbed));
        return true;
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.gravityModifier = 1f;
        if (secondSpark?.destroyed == false) {
            secondSpark.destroySelf();
        }

    }
}
#endregion

#region ■ Do Satsu ━━━━━
public class DragoonExecutingSatsu : MaverickState {
    public X4Dragoon overratedlizard = null!;
    Character? victim;
    public Anim? blackfx; // this is the base of the pillar
    List<Anim> blackPieces = new List<Anim>();  // pillar pieces

    float satsuPunchTime;
    public bool victimWasGrabbedSpriteOnce;
    float timeWaiting;
    bool hasCreatedAnim;


    public DragoonExecutingSatsu(Character grabbedChar) : base("5satsu_grab") {
        victim = grabbedChar;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        overratedlizard = maverick as X4Dragoon ?? throw new NullReferenceException();
        hasCreatedAnim = false;
        maverick.playSound("x4dgn_land", sendRpc: true);
    }

    public override void update() {
        base.update();
        Point? shootPos = maverick.getFirstPOI() ?? maverick.getCenterPos();

        //did the grab connect on the other screen
        if (victim.sprite.name.EndsWith("_grabbed") || victim.sprite.name.EndsWith("_die")) {
            victimWasGrabbedSpriteOnce = true;
        }
        //was it grabbed one? not grabbed sprite anymore? then release
        if (victimWasGrabbedSpriteOnce && !victim.sprite.name.EndsWith("_grabbed")) {
            maverick.changeState(new laposedelpapu());
            return;
        }
        // if not kinda force the grab
        if (!victimWasGrabbedSpriteOnce) {
            timeWaiting += Global.spf;
            if (timeWaiting > 1) {
                victimWasGrabbedSpriteOnce = true;
            }
        }
        //wait the grab to connect
        if (!victimWasGrabbedSpriteOnce) {
            maverick.frameSpeed = 0;
        } else {
            maverick.frameSpeed = 1;

            //continue
            if (maverick.frameIndex > 1) {
                satsuPunchTime -= Global.spf;
                if (!hasCreatedAnim) {
                    hasCreatedAnim = true;
                    createBlackPillar();
                }
                //inverse instead of  += Global.spf; because we want it instantly
                if (satsuPunchTime == 0 && maverick.frameIndex > 3) {
                    satsuPunchTime = 0.05f;

                    if (victim != null) {
                        maverick.playSound("x4dgn_ragin_punch", sendRpc: true);
                        new DragoonSatsuPunchProj(
                            victim.pos.addxy(Helpers.randomRange(-15, 15), Helpers.randomRange(-30, 0)), 1, overratedlizard,
                            player, player.getNextActorNetId(), rpc: true
                        );
                    }
                }
            }

            if (stateTime > SomeoneGettingTheSatsu.maxGrabTime) {
                maverick.changeState(new laposedelpapu());
            }
        }
    }
    private void createBlackPillar() {
        maverick.playSound("x4dgn_special", sendRpc: true); //voice 
        blackfx = new Anim(maverick.pos, "mav_x4dgn_5satsu_black_base",
            maverick.xDir, player.getNextActorNetId(), true, sendRpc: true) {
            zIndex = (ZIndex.Character - 100), ttl = 6f
        };

        //create the pillars above blackfx
        var blackFxPoint = maverick.pos.addxy(0, -11);
        for (int i = 0; i < 22; i++) {
            var blackPiece = new Anim(blackFxPoint.addxy(0, -12 * i), "mav_x4dgn_5satsu_black_piece",
                        maverick.xDir, player.getNextActorNetId(), true, sendRpc: true
                        ) {
                zIndex = (ZIndex.Character - 100 - i), ttl = 6f
            };

            blackPieces.Add(blackPiece);

        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);

        foreach (var piece in blackPieces) {
            if (piece?.destroyed == false) {
                piece.destroySelf();
            }
        }
        if (blackfx?.destroyed == false) {
            blackfx.destroySelf();
        }

        if (victim != null) {
            overratedlizard.meleeWeapon.applyDamage(victim, false, maverick, (int)ProjIds.DragoonSatsuDeath);
        }
        blackPieces.Clear();
        victim?.releaseGrab(maverick);
    }
}
#endregion

#region ■ Receive Satsu ━━
public class SomeoneGettingTheSatsu : GenericGrabbedState {
    public const float maxGrabTime = 3;
    public SomeoneGettingTheSatsu(X4Dragoon grabber) : base(grabber, maxGrabTime, "5satsu_grab", maxNotGrabbedTime: 1f) {
    }
}
#endregion


#region ■ Pose ━━━━━━━
public class laposedelpapu : MaverickState {
    public X4Dragoon overratedlizard = null!;
    public Anim? satsusymbol;
    public laposedelpapu() : base("5satsu_pose") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        overratedlizard = maverick as X4Dragoon ?? throw new NullReferenceException();
        maverick.playSound("x4dgn_ragin_pose", sendRpc: true);
        satsusymbol = new Anim(
        maverick.pos.addxy(0 * -maverick.xDir, -48),
        "mav_x4dgn_5satsu_pose_Fx", 1, player.getNextActorNetId(),
        true, sendRpc: true
    );

    }

    public override void update() {
        base.update();
        if (maverick.isAnimOver()) {
            maverick.changeToIdleRunOrFall();
        }
    }
    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        satsusymbol?.destroySelf();
    }
}

#endregion
#endregion

#region ▄▄▄▄⬤ PROJ ⬤▄▄▄▄








#region ⬤ HDK Weak ━━━━
public class DragoonHadokenWeakProj : Projectile {
    public int type;
    public DragoonHadokenWeakProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4dgn_1atk_proj_weak", netId, player
    ) {
        this.type = type;
        weapon = X4Dragoon.getWeapon();
        projId = (int)ProjIds.DragoonAttackWeakProj;
        damager.damage = 3;
        damager.hitCooldown = 30;
        maxTime = 0.5f;
        destroyOnHit = true;
        destroyOnHitWall = true;
        fadeSprite = "mav_x4dgn_hit_fx";
        playSound("x4dgn_haduken_fire", sendRpc: true);
        //collider here, the deal was to have only one json for the fire sprite
        globalCollider = new Collider(
        new Rect(0, 0, 37, 29).getPoints(),
        true, this, false, false, HitboxFlag.Hitbox, Point.zero);

        switch (type) {
            case 0:
                vel = new Point(250 * xDir, 0);
                break;
            case 1:
                vel = new Point(250 * xDir, 175);
                break;
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new DragoonHadokenWeakProj(
            args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
        );
    }
    float partTime;
    public override void update() {
        base.update();
        partTime += Global.spf;
        if (partTime >= 0.08f) {
            partTime = 0;
            new Anim(
                pos.addxy(0 * xDir, 0),
                "mav_x4dgn_1atk_proj_weak", 1, null, true
            ) {
                //kill sprite before it loops, the loop is used on the proj sprite
                ttl = 0.23f
            };
        }
    }
}
#endregion

#region ⬤ HDK Strong ━━━
public class DragoonHadokenStrongProj : Projectile {  // having only one proj with 4 types
    public int type;                                  // would have asked for a bool in the state
    public DragoonHadokenStrongProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4dgn_1atk_proj_strong", netId, player
    ) {
        this.type = type;
        weapon = X4Dragoon.getWeapon();
        projId = (int)ProjIds.DragoonAttackStrongProj;
        damager.damage = 6;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        maxTime = 0.5f;
        destroyOnHit = true;
        destroyOnHitWall = true;
        fadeSprite = "mav_x4dgn_hit_fx";
        playSound("x4dgn_haduken_fire", sendRpc: true);
        playSound("x4fre_shoot", sendRpc: true);
        //collider here, the deal was to have only one json for the fire sprite
        globalCollider = new Collider(
        new Rect(0, 0, 43, 35).getPoints(),
        true, this, false, false, HitboxFlag.Hitbox, Point.zero);

        switch (type) {
            case 0:
                vel = new Point(750 * xDir, 0);
                break;
            case 1:
                vel = new Point(650 * xDir, 505);
                break;
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new DragoonHadokenStrongProj(
            args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
        );
    }
    float partTime;
    public override void update() {
        base.update();
        partTime += Global.spf;
        if (partTime >= 0.02f) {
            partTime = 0;
            new Anim(
                pos.addxy(0 * xDir, 0),
                "mav_x4dgn_1atk_proj_strong", 1, null, true
            ) {
                //kill sprite before it loops, the loop is used on the proj sprite
                ttl = 0.23f
            };
        }
    }
}
#endregion

#region ⬤ Dragoon Breath ━
public class DragoonBreathProj : Projectile {
    private float timeEffect = 0;
    private float amplitude = 250;
    private float frequency = 2;
    public DragoonBreathProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4dgn_2spcl_Proj", netId, player
    ) {
        weapon = X4Dragoon.getWeapon();
        projId = (int)ProjIds.DragoonBreathProj;
        damager.damage = 2;
        //damager.flinch = Global.defFlinch;
        //damager.hitCooldown = 30;
        vel = new Point(350 * xDir, 0);
        maxTime = 0.7f;
        destroyOnHit = true;
        destroyOnHitWall = true;
        fadeSprite = "mav_x4dgn_2spcl_Proj_fade";
        fadeOnAutoDestroy = true;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new DragoonBreathProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }
    float partTime;
    public override void update() {
        base.update();
        partTime += Global.spf;
        timeEffect += Global.spf;

        float verticalVel = amplitude * MathF.Cos(timeEffect * frequency * MathF.PI * 2);
        vel = new Point(450 * xDir, verticalVel);

        if (partTime > 0.075f) {
            partTime = 0;
            var anim = new Anim(
                pos.addxy(-20 * xDir, -5).addRand(0, 12),
                "mav_x4dgn_1atk_proj_weak", 1, null, true
            ) {
                vel = vel,
                acc = new Point(-vel.x * 2, 0),
                zIndex = this.zIndex - 30,
                ttl = 0.23f
            };
        }
    }
}
#endregion

#region ⬤ Shoryu Proj ━━━
public class DragoonShoryukenProj : Projectile {
    public X4Dragoon overratedlizard = null!;

    public DragoonShoryukenProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4dgn_3dash_proj", netId, player
    ) {
        overratedlizard = owner as X4Dragoon ?? throw new NullReferenceException();
        weapon = X4Dragoon.getWeapon();
        projId = (int)ProjIds.DragoonShoryukenProj;
        damager.damage = 5;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = Point.zero;
        maxTime = 1.2f;
        destroyOnHit = false;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4fre_charged_fade";

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new DragoonShoryukenProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }
    float partTime;
    float zTime;
    public override void update() {
        base.update();
        partTime += Global.spf;
        zIndex = zTime % 2 == 0 ? ZIndex.MainPlayer + 10 : ZIndex.Character - 3110;
        zTime += Global.speedMul;
        if (partTime >= 0.04f) {
            partTime = 0;
            new Anim(pos.addRand(20, 20),
            "mav_x4dgn_3dash_part_fx", -xDir, null, true
         ) {
                vel = new Point(-80 * xDir, 430), //330
                acc = new Point(0, 0)
            };
        }
    }
    public override void postUpdate() {
        base.postUpdate();
        if (owner?.character != null) {
            changePos(owner.character.pos);
        }
    }
    public override void onHitDamagable(IDamagable damagable) {
        base.onHitDamagable(damagable);

        var chr = damagable as Character;
        var mav = damagable as Maverick;

        if (chr != null) {
            chr.yPushVel = -150;
        }
        if (mav != null) {
            mav.yPushVel = -150;
        }
    }
    public override DamagerMessage? onDamage(IDamagable damagable, Player attacker) {
        var chr = damagable as Actor;
        if (chr == null) return null;
        new Anim(
            chr.pos.addxy(Helpers.randomRange(-15, 15) * xDir, Helpers.randomRange(-35, -10)),
            "mav_x4dgn_hit_fx", 1, null, true
        );
        return null;
    }
    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Divekick Proj ━━━
public class DragoonDivekickProj : Projectile {
    public X4Dragoon overratedlizard = null!;

    public DragoonDivekickProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4dgn_3dash_air_proj", netId, player
    ) {
        overratedlizard = owner as X4Dragoon ?? throw new NullReferenceException();
        weapon = X4Dragoon.getWeapon();
        projId = (int)ProjIds.DragoonDivekickProj;
        damager.damage = 5;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = Point.zero;
        maxTime = 1.2f;
        destroyOnHit = false;
        destroyOnHitWall = false;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new DragoonShoryukenProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }
    float partTime;
    float zTime;
    public override void update() {
        base.update();
        if (owner?.character != null) {
            incPos(owner.character.deltaPos);
        }
        partTime += Global.spf;
        zIndex = zTime % 2 == 0 ? ZIndex.MainPlayer + 10 : ZIndex.Character - 3110;
        zTime += Global.speedMul;

        if (partTime >= 0.02f) {
            partTime = 0;
            new Anim(pos.addRand(15, 15),
            "mav_x4dgn_3dash_part_fx", xDir, null, true
         ) {
                vel = new Point(-350 * xDir, -300),
                byteAngle = 96 * xDir //330

            };
        }
    }
    public override void postUpdate() {
        base.postUpdate();
    }
    public override void onHitDamagable(IDamagable damagable) {
        base.onHitDamagable(damagable);

        var chr = damagable as Character;
        var mav = damagable as Maverick;

        if (chr != null) {
            chr.yPushVel = 350;
        }
        if (mav != null) {
            mav.yPushVel = 350;

        }
    }
    public override DamagerMessage? onDamage(IDamagable damagable, Player attacker) {
        var chr = damagable as Actor;
        if (chr == null) return null;
        new Anim(
            chr.pos.addxy(Helpers.randomRange(-15, 15) * xDir, Helpers.randomRange(-35, -10)),
            "mav_x4dgn_hit_fx", 1, null, true
        );
        return null;
    }
    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Giga Aura ━━━━
public class DragoonGigaAuraProj : Projectile {
    public X4Dragoon overratedlizard = null!;
    public int type;

    public DragoonGigaAuraProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(pos, xDir, owner, "mav_x4dgn_4giga_aura", netId, player) {
        overratedlizard = owner as X4Dragoon ?? throw new NullReferenceException();
        this.type = type;
        weapon = X4Dragoon.getWeapon();
        projId = (int)ProjIds.DragoonGigaAuraProj;
        damager.damage = 12;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 40;
        vel = Point.zero;
        maxTime = 4.2f;
        destroyOnHit = true;
        destroyOnHitWall = false;

        switch (type) {
            case 0:
                changeSprite("mav_x4dgn_4giga_aura", false);
                break;
            case 1:
                changeSprite("mav_x4dgn_4giga_aura_air", false);
                break;
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new DragoonGigaAuraProj(
            args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
        );
    }

    float zTime;
    float partTime;
    public override void update() {
        base.update();
        zIndex = zTime % 2 == 0 ? ZIndex.MainPlayer + 10 : ZIndex.Character - 3110;
        zTime += Global.speedMul;
        partTime += Global.spf;

        if (partTime >= 0.05f) {
            partTime = 0;
            // diff random range either ground or air, looks cooler
            if (type == 0) {
                //grounded part
                new Anim(this.pos.addxy(Helpers.randomRange(-60, 60), Helpers.randomRange(-25, 15)), "mav_x4dgn_3dash_part_fx",
                    xDir, Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true) {
                    vel = new Point(0, -200), acc = Point.zero, yDir = -1,
                };
            } else {
                //airborne part
                new Anim(this.pos.addxy(Helpers.randomRange(-60, 60), Helpers.randomRange(-80, 20)), "mav_x4dgn_3dash_part_fx",
                    xDir, Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true) {
                    vel = new Point(0, -200), acc = Point.zero, yDir = -1,
                };
            }
        }
    }
    public override void postUpdate() {
        base.postUpdate();
        if (owner?.character != null) {
            incPos(owner.character.deltaPos);
        }
    }
    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion
#region ⬤ Meteor Gen ━━━
public class DragoonMeteorGenerator : Projectile {
    public int receivedMeteors;

    float spawnTimer;
    const float INTERVAL = 0.5f;

    public DragoonMeteorGenerator(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        weapon = X4Dragoon.getWeapon();
        projId = (int)ProjIds.DragoonMeteorGenerator;
        damager.damage = 0;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 0;
        vel = new Point(0 * xDir, 0);
        maxTime = 15f;
        destroyOnHit = false;
        destroyOnHitWall = false;
        //fadeSprite = "mmx_x4btr_lv0_fade";

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new DragoonMeteorGenerator(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();

        if (!ownedByLocalPlayer) return;

        spawnTimer += Global.spf;
        //small delay before start spawning
        if (time >= 3f && spawnTimer >= INTERVAL) {
            if (receivedMeteors > 0) {
                int minRange = (xDir == 1) ? -700 : -225; // the bigger pos.y, the bigger the 550
                int maxRange = (xDir == 1) ? 225 : 700;
                //TODO add small Y variation
                Point spawnPos = new Point(
                    pos.x + (Helpers.randomRange(minRange, maxRange)),
                    pos.y - (Helpers.randomRange(220, 300))
                );

                new DragoonDownwardsMeteor(
                spawnPos, xDir, this,
                owner, owner.getNextActorNetId(), rpc: true);
                spawnTimer = 0f;
                receivedMeteors--;
            }
        }
    }
    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion


#region ⬤ Up meteor ━━━━
public class DragoonUpwardsMeteor : Projectile {
    public DragoonMeteorGenerator? gen;

    public DragoonUpwardsMeteor(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mmx_x4fre_uncharged", netId, player
    ) {
        weapon = X4Dragoon.getWeapon();
        projId = (int)ProjIds.DragoonUpwardsMeteor;
        damager.damage = 4;
        damager.flinch = Global.defFlinch;
        //damager.hitCooldown = 30;
        int randomX = new Random().Next(-140, 141);
        int randomY = new Random().Next(-500, -300);
        vel = new Point(randomX, randomY);
        maxTime = 1f;
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mav_x4dgn_hit_fx";

        //to not do a json, we reuse x's rising fire
        frameIndex = 3;
        xScale = 0.1f;
        yScale = 0.1f;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new DragoonUpwardsMeteor(
            args.pos, args.xDir, args.owner, args.player, args.netId //?? idk if the null is safe
        );
    }

    public override void update() {
        base.update();
        //increase scale and clamp at 1 chupalo cat
        xScale = Math.Min(xScale + Global.spf * 10f, 1);
        yScale = Math.Min(yScale + Global.spf * 10f, 1);
        if (time >= 0.5f) {
            //start fading
            visible = Global.isOnFrameCycle(3);
            vel.x = Helpers.lerp(vel.x, 0, Global.spf * 4f);
            vel.y = Helpers.lerp(vel.y, 0, Global.spf * 4f);
        }
        if (time >= 0.9f) {
            //die but not play the hit effect
            new Anim(pos.addxy(0 * xDir, 0), "mav_x4dgn_4giga_proj_star", 1, null, true);
            destroySelfNoEffect();
            if (gen != null) {
                gen.receivedMeteors++;
            }
        }
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Down Meteor ━━
public class DragoonDownwardsMeteor : Projectile {
    private bool hasFallen;

    public DragoonDownwardsMeteor(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4dgn_4giga_Proj_3starting", netId, player
    ) {
        weapon = X4Dragoon.getWeapon();
        projId = (int)ProjIds.DragoonDownwardsMeteor;
        damager.damage = 4;
        damager.flinch = Global.defFlinch;
        //damager.hitCooldown = 30;
        vel = Point.zero;
        maxTime = 2.7f; //falling would have maxtime of 2f
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mav_x4dgn_hit_fx";

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new DragoonDownwardsMeteor(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();

        if (time >= 0.7f && !hasFallen) {
            hasFallen = true;
            new Anim(pos, "mav_x4dgn_4giga_proj_star", 1, null, true) { vel = new Point(275 * xDir, 200) };
            changeSprite("mav_x4dgn_4giga_Proj_4falling", false);
            vel = new Point(275 * xDir, 200);
        }
        if (time >= 2.5f) {
            destroyOnHitWall = true;
        }
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}

#endregion

#region ⬤ Satsu Punch ━━━
public class DragoonSatsuPunchProj : Projectile {
    float partTime;
    public DragoonSatsuPunchProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4dgn_5satsu_Proj", netId, player
    ) {
        weapon = X4Dragoon.getWeapon();
        damager.damage = 0f;
        vel = new Point(0 * xDir, 0);
        projId = (int)ProjIds.DragoonSatsuPunchProj;
        maxTime = 0.09f;
        destroyOnHit = false;
        destroyOnHitWall = false;


        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }
    public static Projectile rpcInvoke(ProjParameters args) {
        return new DragoonSatsuPunchProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
        partTime += Global.spf;
        if (partTime >= 0.1f) {
            partTime = 0;
            new Anim(
                pos.addxy(0 * xDir, 0),
                "mav_x4dgn_1atk_proj_strong", 1, null, true
            ) {
                vel = new Point(0, -450),
                acc = new Point(0, 0),
                zIndex = (ZIndex.Character - 420)
            };
        }
    }
}
#endregion
#endregion

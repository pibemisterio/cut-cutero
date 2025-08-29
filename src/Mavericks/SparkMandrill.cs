﻿using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SparkMandrill : Maverick {
	public SparkMPunchWeapon punchWeapon = new();
	public SparkMSparkWeapon sparkWeapon = new();
	public SparkMStompWeapon stompWeapon = new();
	public static Weapon netWeapon = new Weapon(WeaponIds.SparkMGeneric, 94);

	public SparkMandrill(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(SparkMPunchState), new(60, true, true) },
			{ typeof(SparkMDashPunchState), new(45, false, true) },
			{ typeof(MShoot), new(2 * 60, true, true) }
		};
		spriteToCollider["dash_punch"] = getDashCollider();

		weapon = new Weapon(WeaponIds.SparkMGeneric, 94);

		awardWeaponId = WeaponIds.ElectricSpark;
		weakWeaponId = WeaponIds.ShotgunIce;
		weakMaverickWeaponId = WeaponIds.ChillPenguin;

		netActorCreateId = NetActorCreateId.X4Dragoon;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		usesAmmo = false;
		canHealAmmo = false;
		ammo = 32;
		maxAmmo = 32;
		grayAmmoLevel = 31;
		ammoRoundDown = true;
		barIndexes = (55, 44);

		armorClass = ArmorClass.Heavy;
		canStomp = true;
	}

	public override void update() {
		base.update();

		//rechargeAmmo(8);
		subtractTargetDistance = 70;
		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (specialPressed()) {
					//if (ammo >= 32)
					{
						changeState(getShootState());
					}
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new SparkMDashPunchState());
				} else if (shootPressed()) {
					changeState(new SparkMPunchState());
				}
			} else if (state is MJump || state is MFall) {
				if (input.isHeld(Control.Up, player)) {
					var hit = Global.level.checkTerrainCollisionOnce(this, 0, -15);
					if (vel.y < 0 && hit?.gameObject is Wall wall && !wall.topWall) {
						changeState(new SparkMClimbState(hit.getHitPointSafe()));
					}
				}
			}
		}
	}

	public override string getMaverickPrefix() {
		return "sparkm";
	}

	public MaverickState getShootState() {
		return new MShoot((Point pos, int xDir) => {
			shakeCamera(sendRpc: true);
			new TriadThunderProjCharged(pos, xDir, 1, this, player, player.getNextActorNetId(), rpc: true);
			new TriadThunderProjCharged(pos, -xDir, 1, this, player, player.getNextActorNetId(), rpc: true);
		}, "sparkmSparkX1");
	}

	public override MaverickState[] strikerStates() {
		return [
			getShootState(),
			new SparkMPunchState(),
			new SparkMDashPunchState()
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		List<MaverickState> aiStates = [
			new SparkMDashPunchState(),
			getShootState(),
		];
		if (enemyDist <= 60) {
			aiStates.Add(new SparkMPunchState());
		}
		return aiStates.ToArray();
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Punch,
		DashPunch,
		Shoot,
		Fall,	
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"sparkm_punch" => MeleeIds.Punch,
			"sparkm_dash_punch" => MeleeIds.DashPunch,
			"sparkm_shoot" => MeleeIds.Shoot,
			"sparkm_fall" => MeleeIds.Fall,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Punch => new GenericMeleeProj(
				punchWeapon, pos, ProjIds.SparkMPunch, player,
				4, Global.defFlinch, 45, addToLevel: addToLevel
			),
			MeleeIds.DashPunch => new GenericMeleeProj(
				punchWeapon, pos, ProjIds.SparkMPunch, player,
				4, Global.defFlinch, 45, addToLevel: addToLevel
			),
			MeleeIds.Shoot => new GenericMeleeProj(
				sparkWeapon, pos, ProjIds.SparkMSpark, player,
				4, Global.defFlinch, addToLevel: addToLevel
			),
			MeleeIds.Fall => new GenericMeleeProj(
				stompWeapon, pos, ProjIds.SparkMStomp, player,
				2, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}
	public override void updateProjFromHitbox(Projectile proj) {
		if (proj.projId == (int)ProjIds.SparkMStomp) {
			float damage = Helpers.clamp(MathF.Floor(deltaPos.y * 0.9f), 1, 4);
			proj.damager.damage = damage;
		}
	}

}

#region weapons
public class SparkMSparkWeapon : Weapon {
	public SparkMSparkWeapon() {
		index = (int)WeaponIds.SparkMSpark;
		killFeedIndex = 94;
	}
}

public class SparkMStompWeapon : Weapon {
	public SparkMStompWeapon() {
		index = (int)WeaponIds.SparkMStomp;
		killFeedIndex = 94;
	}
}

public class SparkMPunchWeapon : Weapon {
	public SparkMPunchWeapon() {
		index = (int)WeaponIds.SparkMPunch;
		killFeedIndex = 94;
	}
}
#endregion

#region states
public class SparkMPunchState : MaverickState {
	public float dustTime;
	public SparkMPunchState() : base("punch") {
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}
}

public class SparkMDashPunchState : MaverickState {
	public float dustTime;
	public SparkMDashPunchState() : base("dash_punch") {
	}

	public override void update() {
		base.update();
		if (player == null) return;

		var move = new Point(250 * maverick.xDir, 0);

		var hitGround = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 5, 20);
		if (hitGround == null) {
			maverick.changeState(new MIdle());
			return;
		}

		var hitWall = Global.level.checkTerrainCollisionOnce(maverick, move.x * Global.spf * 2, -5);
		if (hitWall?.isSideWallHit() == true) {
			maverick.playSound("crash", sendRpc: true);
			maverick.shakeCamera(sendRpc: true);
			maverick.changeState(new MIdle());
			return;
		}

		maverick.move(move);

		if (stateTime > 0.6) {
			maverick.changeState(new MIdle());
			return;
		}

		dustTime += Global.spf;
		if (dustTime > 0.1) {
			dustTime = 0;
			new Anim(maverick.pos.addxy(0, -4), "dust", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
		}
	}
}

public class SparkMClimbState : MaverickState {
	Point hitPoint;
	float climbSpeed = 100;
	public SparkMClimbState(Point hitPoint) : base("climb") {
		this.hitPoint = hitPoint;
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (input.isPressed(Control.Jump, player) || input.isPressed(Control.Down, player)) {
			maverick.changeState(new MFall());
			return;
		}

		bool leftHeld = input.isHeld(Control.Left, player);
		bool rightHeld = input.isHeld(Control.Right, player);

		Point moveAmount = new Point();
		if (leftHeld) {
			maverick.xDir = -1;
			moveAmount.x = -climbSpeed * Global.spf;
		} else if (rightHeld) {
			maverick.xDir = 1;
			moveAmount.x = climbSpeed * Global.spf;
		}

		if (moveAmount.x != 0) {
			maverick.move(moveAmount, useDeltaTime: false);

			// Get amount to snap up to ceiling
			Point origin = maverick.pos.addxy(0, 0);
			Point dest = origin.addxy(0, -maverick.height - 5);
			var ceiling = Global.level.raycast(origin, dest, new List<Type> { typeof(Wall) });
			if (ceiling?.gameObject is Wall wall && !wall.topWall) {
				float newY = ceiling.getHitPointSafe().y + maverick.height;
				if (MathF.Abs(newY - maverick.pos.y) > 1) {
					maverick.changePos(new Point(maverick.pos.x, newY));
				}
			} else {
				maverick.changeState(new MFall());
				return;
			}

			maverick.frameSpeed = 1;
		} else {
			maverick.frameSpeed = 0;
			maverick.frameIndex = 0;
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.useGravity = false;
		maverick.stopMovingS();
		maverick.changePos(new Point(maverick.pos.x, hitPoint.y + maverick.height));
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
	}
}

public class SparkMFrozenState : MaverickState {
	public SparkMFrozenState() : base("freeze") {
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (maverick.frameIndex >= 1 && !once) {
			once = true;
			maverick.breakFreeze(player, maverick.getCenterPos(), sendRpc: true);
		}

		if (maverick.isAnimOver()) {
			maverick.changeState(new MIdle());
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
	}
}
#endregion

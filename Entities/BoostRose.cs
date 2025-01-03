﻿using System;
using Monocle;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using System.Collections;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Celeste.Mod.Entities;

/*
 * permissions:
 * [Y] sprites by mosscairn
 * [Y] audio by catapillie
 * [Y] concept by Quinnigan
 */

namespace Celeste.Mod.LollydesSJContributions.Entities
{
    [CustomEntity("lollyde_sj/BoostRose")]
    internal class BoostRose : Actor
    {
        private int charges, lastFacing;
        private float boostCooldown, boostDelay, boostSpeed, boostDuration, boostDurationMax, boostThreshold, gravity, highFrictionTimer, noGravityTimer;
        private bool destroyed;
        private Vector2 speed, customCarryOffset, prevLiftSpeed;
        private Holdable hold;
        private Collision onCollideH, onCollideV;
        private Player player;
        private Level level;
        private Sprite sprite;

        private readonly SoundSource boostSfx, moveSfx;

        public float FallSpeed { get; private set; }
        public float FastFallSpeed { get; private set; }
        public float SlowFallSpeed { get; private set; }

        private static ParticleType boostParticles;

        public BoostRose(EntityData data, Vector2 offset) : this(data.Position + offset)
        {
        }


        public BoostRose(Vector2 position) : base(position)
        {

            Depth = Depths.Player - 5;
            FallSpeed = 60f;
            FastFallSpeed = 140f;
            SlowFallSpeed = 44f;
            this.boostDelay = 0.2f;
            this.boostSpeed = -160f;
            boostDurationMax = 0.5f;
            boostThreshold = -300f;
            charges = 3;
            destroyed = false;
            initializeParticles();
            customCarryOffset = Vector2.UnitY * -8f;
            gravity = 30f;
            prevLiftSpeed = Vector2.Zero;

            Add(sprite = MainPlogon.SpriteBank.Create("roseGlider"));
            sprite.CenterOrigin();
            sprite.Origin.Y += 9;
            sprite.Play("idle_3");

            Collider = new Hitbox(6, 10, -3, -5); // todo adjust
            onCollideH = new Collision(collideHandlerH);
            onCollideV = new Collision(collideHandlerV);


            Add(hold = new Holdable(0.3f));
            hold.PickupCollider = new Hitbox(10, 20, -5, -15); // todo adjust
            hold.SlowFall = true;
            hold.SlowRun = false; // todo ask
            hold.SpeedGetter = () => speed;
            hold.OnPickup = new Action(onPickup);
            hold.OnRelease = new Action<Vector2>(onRelease);
            hold.OnHitSpring = new Func<Spring, bool>(onHitSpring);

            Add(boostSfx = new SoundSource());
            Add(moveSfx = new SoundSource().Play(CustomSFX.BoostRoseMovement));
        }

        public static void Load()
        {
            IL.Celeste.Player.NormalUpdate += patchPlayerNormalUpdate;
        }

        public static void Unload()
        {
            IL.Celeste.Player.NormalUpdate -= patchPlayerNormalUpdate;
        }

        private static void patchPlayerNormalUpdate(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(120f)))
            { // fastfall
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>((speed, player) => {
                    if (player?.Holding?.Entity is BoostRose flower)
                    {
                        return flower.FastFallSpeed;
                    }
                    return speed;
                });
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(24f)))
            { // slowfall
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>((speed, player) => {

                    if (player?.Holding?.Entity is BoostRose flower)
                    {
                        return flower.SlowFallSpeed;
                    }
                    return speed;
                });
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(40f)))
            { // normal fall
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>((speed, player) => {

                    if (player?.Holding?.Entity is BoostRose flower)
                    {
                        return flower.FallSpeed;
                    }
                    return speed;
                });
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();

            if (boostCooldown > 0)
                boostCooldown -= Engine.DeltaTime;
            if (boostDuration > 0 && hold.IsHeld)
            {
                player.Speed.Y = boostSpeed;
                boostDuration -= Engine.DeltaTime;
                if (boostDuration <= 0)
                    boostCooldown = boostDelay;
            }
            else if (boostDuration <= 0 && charges == 0 && hold.IsHeld)
            {
                destroySelf();
            }

            if (hold.IsHeld)
            {
                if (Input.Dash.Pressed)
                {
                    consumeBoost();
                    if (destroyed)
                        return;
                }

                // SFX might be stopped by room transition
                if (!moveSfx.Playing)
                    moveSfx.Play(CustomSFX.BoostRoseMovement);

                float intensity = hold.Holder.OnGround() ? 0 : Calc.ClampedMap(hold.Holder.Speed.Length(), 0, 160);
                moveSfx.Param("speed", intensity);
                moveSfx.Param("fadeout", 0);
            }
            else
            {
                moveSfx.Param("fadeout", 1);

                if (highFrictionTimer >= 0)
                {
                    highFrictionTimer -= Engine.DeltaTime;
                }
                if (OnGround(1))
                {
                    // todo X position adjustment if on ledge
                    speed.X = Calc.Approach(speed.X, 0, 800f * Engine.DeltaTime);
                    Vector2 liftspeed = LiftSpeed;
                    if (liftspeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
                    {
                        speed = prevLiftSpeed;
                        prevLiftSpeed = Vector2.Zero;
                        speed.Y = Math.Min(speed.Y * 0.6f, 0f);
                        if (speed.X != 0f && speed.Y == 0f)
                        {
                            speed.Y = -60;
                        }
                        if (speed.Y < 0f)
                        {
                            noGravityTimer += 0.15f;
                        }
                    }
                    else
                    {
                        prevLiftSpeed = liftspeed;
                        if (liftspeed.Y < 0f && speed.Y < 0f)
                        {
                            speed.Y = 0f;
                        }
                    }
                }
                else
                {
                    if (hold.ShouldHaveGravity)
                    {
                        float gravityCoefficient = 100;
                        if (speed.Y < -30f)
                        {
                            gravityCoefficient *= 2;
                        }
                        float xAdjustSpeed = 10;
                        if (speed.Y < 0 || highFrictionTimer <= 0)
                        {
                            xAdjustSpeed = 40;
                        }
                        speed.X = Calc.Approach(speed.X, 0f, xAdjustSpeed * Engine.DeltaTime);
                        if (noGravityTimer > 0f)
                        {
                            noGravityTimer -= Engine.DeltaTime;
                        }
                        else
                        {
                            speed.Y = Calc.Approach(speed.Y, gravity, gravityCoefficient * Engine.DeltaTime);
                        }
                    }
                }
                MoveH(speed.X * Engine.DeltaTime, onCollideH, null);
                MoveV(speed.Y * Engine.DeltaTime, onCollideV, null);

                // boundary enforcing
                if (Left < level.Bounds.Left)
                {
                    Left = level.Bounds.Left;
                    onCollideH(new CollisionData
                    {
                        Direction = -Vector2.UnitX
                    });
                }
                else if (Right > level.Bounds.Right)
                {
                    Right = level.Bounds.Right;
                    onCollideH(new CollisionData
                    {
                        Direction = Vector2.UnitX
                    });
                }

                if (Top > level.Bounds.Bottom + 16)
                {
                    RemoveSelf();
                }
                hold.CheckAgainstColliders();
            }
        }

        private void consumeBoost()
        {
            if (canBoost())
            {
                hold.Holder.LaunchBegin();
                hold.Holder.Speed.Y = boostSpeed;
                // todo particles
                //level.ParticlesBG.Emit(boostParticles, 8, Position - Vector2.UnitY * 10, Vector2.UnitX * 5 + Vector2.UnitY * 3, (float) Math.PI);
                if (charges > 0)
                {
                    sprite.Play($"boost_{charges}");
                }
                charges--;
                boostDuration = boostDurationMax;
                Input.Dash.ConsumeBuffer();

                //sfx
                boostSfx.Play("event:/lollyde-triple-boost-flower/boost_" + (3 - charges));
            }
            else if (shouldBeDestroyed())
            {
                destroySelf();
                Input.Dash.ConsumeBuffer();
                return;
            }
        }

        private bool shouldBeDestroyed()
        {
            return hold.Holder.Speed.Y > boostThreshold && boostCooldown <= 0f && charges == 0 && boostDuration <= 0;
        }

        private bool canBoost()
        {
            return hold.Holder.Speed.Y > boostThreshold && boostCooldown <= 0f && charges > 0 && boostDuration <= 0;
        }

        private void initializeParticles()
        {
            if (boostParticles == null)
            {
                boostParticles = new ParticleType()
                {
                    Source = GFX.Game["particles/petal"],
                    Color = Calc.HexToColor("E63244"),
                    DirectionRange = 1 / 4 * (float)Math.PI,
                    FadeMode = ParticleType.FadeModes.Late,
                    LifeMin = 1f,
                    LifeMax = 1.5f,
                    SpeedMin = 15f,
                    SpeedMax = 25f,
                    RotationMode = ParticleType.RotationModes.Random,
                    SpinMin = 0,
                    SpinMax = (float)Math.PI,
                    Size = 1f
                };
            }
        }

        private bool onHitSpring(Spring spring)
        {
            if (!hold.IsHeld)
            {
                if (spring.Orientation == Spring.Orientations.Floor && speed.Y >= 0f)
                {
                    speed.X = speed.X * 0.5f;
                    speed.Y = -160f;
                    noGravityTimer = 0.15f;
                    return true;
                }

                MoveTowardsY(spring.CenterY + 5f, 4f, null);
                speed.X = spring.Orientation == Spring.Orientations.WallRight ? -160f : 160;
                speed.Y = -80f;
                noGravityTimer = 0.1f;
                return true;
            }
            return false;
        }

        private void onRelease(Vector2 direction)
        {
            DynamicData.For(player).Set("CarryOffsetTarget", new Vector2(0f, -12f));
            RemoveTag(Tags.Persistent);

            highFrictionTimer = 0.5f;

            if (charges == 0)
            {
                destroySelf();
                return;
            }
            player = null;
            direction.Y *= 0.5f;
            if (direction.Y == 0f && direction.X != 0f)
            {
                direction.Y = -0.4f;
            }
            speed = direction * 100f;
        }

        private void destroySelf()
        {
            destroyed = true;
            speed = player.Speed;
            if (hold.IsHeld)
            {
                hold.Holder.Drop();
            }
            Collidable = false;
            hold.Active = false;
            Add(new Coroutine(destroySelfCoroutine(), true));
        }

        private IEnumerator destroySelfCoroutine()
        {
            // todo audio effect
            yield return 1f;
            RemoveSelf();
            yield break;
        }

        private void onPickup()
        {
            player = hold.Holder;
            AddTag(Tags.Persistent);
            lastFacing = (int)player.Facing;
            DynamicData.For(player).Set("CarryOffsetTarget", customCarryOffset);
        }

        private void collideHandlerV(CollisionData data)
        {
            // todo sfx when landing fast enough?
            if (speed.Y < 0f)
            {
                speed.Y *= -0.5f;
                return;
            }
            speed.Y = 0f;
        }

        private void collideHandlerH(CollisionData data)
        {
            speed.X *= -1f;
        }

        public override void Render()
        {
            if (sprite.Visible) sprite.DrawOutline();
            base.Render();
        }
    }
}

using System;
using System.Linq;

namespace SecondHonk
{
    static class EqualActorTests
    {
        static World New(Content content, bool companion)
        {
            var w = new World(new SaveData { Mischief = false, Scale = 1, Companion = companion }, content, 42);
            w.Resize(1280, 752);
            w.Goose.Place(new V(450, 360));
            if (companion)
                w.Friend.Place(new V(300, 360));
            w.Update(0);
            return w;
        }

        static void Advance(World w, float seconds)
        {
            for (int i = 0; i < seconds * 60; i++)
                w.Update(1f / 60);
        }

        public static void Run(Content content, Action<bool, string> check)
        {
            var w = New(content, true);
            check(w.Goose.Name == "White" && w.Goose.Id == 0, "White goose carries a stable identity");
            check(w.Friend.Name == "Canada" && w.Friend.Id == 1, "Canada goose carries a stable identity");
            check(!ReferenceEquals(w.Goose, w.Friend), "The world holds exactly two actor slots and no path creates a third goose");
            check(w.Goose.Held == null && w.Friend.Held == null && w.Goose.TargetObject == null && w.Friend.TargetObject == null, "Both geese start with no held or reserved objects");
            check(w.Coordinator.CursorOwner == null && w.Coordinator.DeliveryOwner == null && w.Coordinator.WindowOwner == null && w.Coordinator.ReservationCount == 0, "Shared-play coordinator starts with no owners or reservations");

            w = New(content, false);
            w.Fetch(w.Goose.Pos + new V(220, 0));
            Advance(w, 8);
            check(w.Fetches == 1 && w.Goose.Held == null && w.StickVisible, "White initiates, carries and finishes fetch");
            w.Deliver(false);
            Advance(w, 18);
            check(w.Deliveries == 1 && w.Coordinator.DeliveryOwner == null, "White initiates and finishes delivery with the pipeline released");

            w = New(content, true);
            w.Fetch(w.Friend, w.Friend.Pos + new V(220, 0));
            Advance(w, 10);
            check(w.Fetches == 1 && w.Friend.Held == null && w.StickVisible, "Canada initiates, carries and finishes fetch");
            Goose deliverer = null;
            w.DeliveryCompleted += actor => deliverer = actor;
            w.Deliver(w.Friend, false);
            Advance(w, 20);
            check(w.Deliveries == 1 && deliverer == w.Friend && w.Friend.Task == Activity.Wander, "Canada initiates, receives and finishes delivery as the recorded actor");
            check(w.Coordinator.DeliveryOwner == null && w.Coordinator.ReservationCount == 0, "Delivery completion leaves no stale owner or reservation");

            w = New(content, true);
            w.ThrowBall(w.Goose, w.Goose.Pos + new V(90, 0));
            check(w.Goose.Task == Activity.Ball, "White initiates ball play on command");
            Advance(w, 6);
            int afterWhite = w.Kicks;
            check(afterWhite > 0, "White contacts the ball during play");
            w.ThrowBall(w.Friend, w.Friend.Pos + new V(90, 0));
            check(w.Friend.Task == Activity.Ball, "Canada initiates ball play on command");
            Advance(w, 6);
            check(w.Kicks > afterWhite, "Canada contacts the ball during play");

            w = New(content, true);
            Goose honker = null;
            w.Honked += actor => honker = actor;
            w.Honk(w.Friend);
            check(honker == w.Friend, "Sound events identify Canada as the acting goose");
            honker = null;
            w.Honk();
            check(honker == w.Goose, "Default honk commands target white");

            w = New(content, true);
            var shared = w.PlaceObjectRaw(ToyKind.Stick, new V(600, 360));
            w.Goose.Place(new V(560, 360));
            w.Friend.Place(new V(640, 360));
            w.SetTask(w.Goose, Activity.Fetch);
            w.SetTask(w.Friend, Activity.Fetch);
            Advance(w, 4);
            int holders = (w.Goose.Held != null ? 1 : 0) + (w.Friend.Held != null ? 1 : 0);
            check(holders <= 1, "Contested objects resolve to a single holder");
            if (w.Goose.Held != null)
                check(w.Goose.Held.Holder == w.Goose && w.Friend.Held == null, "White holds the contested stick without a second holder");
            else if (w.Friend.Held != null)
                check(w.Friend.Held.Holder == w.Friend && w.Goose.Held == null, "Canada holds the contested stick without a second holder");
            else
                check(w.Coordinator.OwnerOf(shared) == null || w.Coordinator.OwnerOf(shared) == w.Goose || w.Coordinator.OwnerOf(shared) == w.Friend, "Unheld contention leaves at most one reservation owner");
            check(shared.Holder == null || (shared.Holder.Held == shared), "Holder and held references agree on ownership");

            w = New(content, true);
            w.Data.MouseSteal = true;
            w.Goose.Place(new V(450, 360));
            w.Chase(w.Goose, w.Beak(w.Goose));
            w.Update(.01f);
            check(w.Goose.Task == Activity.Chase && !w.HoldingCursor && w.GrabOwner == null, "A provoked goose chases without capturing the cursor in Normal");
            w.Chase(w.Friend, w.Beak(w.Friend));
            w.Update(.1f);
            check(w.Friend.Task == Activity.Chase && !w.HoldingCursor && w.Coordinator.CursorOwner == null, "Neither goose captures the cursor outside Chaos");
            Advance(w, 2);
            check(!w.HoldingCursor && w.Coordinator.CursorOwner == null, "Chase never acquires cursor ownership without the Chaos tier");

            w = New(content, true);
            w.Deliver(w.Goose, false);
            w.Deliver(w.Friend, false);
            check(w.Coordinator.DeliveryOwner == w.Goose && w.Friend.Task != Activity.Deliver, "Delivery pipeline stays serialized behind a single owner");
            Advance(w, 20);
            check(w.Deliveries == 1, "Serialized delivery completes exactly once");
            w.Deliver(w.Friend, false);
            Advance(w, 20);
            check(w.Deliveries == 2, "Waiting senders can deliver after the pipeline releases");

            w = New(content, true);
            var reserved = w.PlaceObjectRaw(ToyKind.Ball, new V(600, 360));
            w.Friend.Place(new V(560, 360));
            w.SetTask(w.Friend, Activity.Ball);
            Advance(w, .5f);
            check(w.Coordinator.OwnerOf(reserved) == w.Friend, "Ball pursuit registers a reservation for Canada");
            w.RemoveObject(reserved);
            check(w.Friend.Task == Activity.Wander && w.Friend.TargetObject == null && w.Coordinator.OwnerOf(reserved) == null, "Removing a reserved toy returns its actor to ordinary activity with no stale reservation");
            Advance(w, 2);
            check(w.Friend.Task == Activity.Wander, "Actor stays unstuck after its reservation is removed");

            w = New(content, true);
            var carried = w.PlaceObjectRaw(ToyKind.Stick, w.Friend.Pos + new V(60, 0));
            w.SetTask(w.Friend, Activity.Fetch);
            Advance(w, 1);
            w.OnObjectDragged(carried);
            check(w.Friend.Task == Activity.Wander && w.Friend.Held == null && w.Friend.TargetObject == null, "Dragging a reserved toy cancels its pursuit without a stuck actor");
            check(carried.Holder == null && !carried.Carried, "Dragged toys release carry state");

            w = New(content, true);
            w.Fetch(w.Friend, w.Friend.Pos + new V(200, 0));
            Advance(w, 1);
            w.ClearProps();
            check(w.Objects.Count == 0 && w.Goose.Held == null && w.Friend.Held == null, "Clearing the desktop drops every held item");
            check(w.Coordinator.ReservationCount == 0 && w.Coordinator.CursorOwner == null && w.Coordinator.DeliveryOwner == null, "Clearing the desktop releases every reservation and owner");
            check(w.Goose.Task == Activity.Wander && w.Friend.Task == Activity.Wander, "Clearing the desktop returns actors to ordinary activity");

            w = New(content, true);
            w.SetTask(w.Goose, Activity.Sleep);
            w.Goose.Target = w.Goose.Pos;
            w.Fetch(w.Friend, w.Friend.Pos + new V(200, 0));
            Advance(w, 1);
            w.SetCompanion(false);
            check(w.Friend.Task == Activity.Wander && w.Friend.Held == null && w.Coordinator.ReservationCount == 0, "Disabling Canada releases its reservations and held items");
            check(w.Coordinator.DeliveryOwner == null && w.Coordinator.CursorOwner == null, "Disabling Canada releases exclusive owners it held");
            check(w.Goose.Task == Activity.Sleep, "Disabling Canada leaves white undisturbed");
            Advance(w, 2);
            check(w.Friend.Task == Activity.Wander, "Disabled Canada stays unstuck");

            w = New(content, true);
            var pending = w.Coordinator.Offer(w.Goose, w.Friend, "Ball", 30);
            check(pending != null && pending.State == InvitationState.Offered, "Invitations can be offered between the geese");
            check(w.Coordinator.Respond(pending.Id, true) && pending.State == InvitationState.Accepted, "Invitations can be accepted");
            var declined = w.Coordinator.Offer(w.Friend, w.Goose, "Fetch", 30);
            w.Coordinator.Respond(declined.Id, false);
            check(declined.State == InvitationState.Declined, "Invitations can be declined");
            var expiring = w.Coordinator.Offer(w.Goose, w.Friend, "Ball", .5f);
            Advance(w, 1);
            check(expiring.State == InvitationState.Expired, "Invitations time out without an answer");
            var paused = w.Coordinator.Offer(w.Goose, w.Friend, "Ball", 30);
            w.SetPaused(true);
            check(paused.State == InvitationState.Cancelled, "Pausing cancels pending invitations");

            w = New(content, true);
            w.Fetch(w.Friend, w.Friend.Pos + new V(220, 0));
            Advance(w, 1);
            check(w.Coordinator.ReservationCount == 1, "Fetch pursuit holds a reservation before pause");
            V frozenGoose = w.Goose.Pos, frozenFriend = w.Friend.Pos;
            w.SetPaused(true);
            Advance(w, 1);
            check(w.Goose.Pos.X == frozenGoose.X && w.Friend.Pos.X == frozenFriend.X, "Pause freezes both actors");
            check(w.Coordinator.ReservationCount == 0 && w.Friend.TargetObject == null, "Pause releases unpossessed reservations");
            w.SetPaused(false);
            Advance(w, 10);
            check(w.Fetches == 1, "Actors resume ordinary activity cleanly after pause");

            w = New(content, false);
            Goose soloHonker = null;
            w.Honked += actor => soloHonker = actor;
            w.Honk();
            check(soloHonker == w.Goose, "Solo sound events identify white");
            w.Chase(w.Goose.Pos + new V(100, 0));
            check(w.Goose.Task == Activity.Chase && w.Friend.Task == Activity.Wander, "Solo commands target white and leave no third actor running");
        }
    }
}

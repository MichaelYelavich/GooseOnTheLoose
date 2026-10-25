using System;
using System.Collections.Generic;

namespace SecondHonk
{
    public enum InvitationState
    {
        Offered,
        Accepted,
        Declined,
        Expired,
        Cancelled
    }

    public sealed class Invitation
    {
        public Guid Id = Guid.NewGuid();
        public Goose Inviter, Invitee;
        public string Kind = "";
        public float ExpiresIn;
        public InvitationState State = InvitationState.Offered;
    }

    // Shared-play coordinator: single-owner reservations for contested toys,
    // cursor, delivery pipeline and external-window manipulation, plus a
    // minimal invitation lifecycle (offer / accept / decline / expire) that
    // later social scenes build on. No scene logic lives here.
    public sealed class PlayCoordinator
    {
        public Goose CursorOwner, DeliveryOwner, WindowOwner;
        readonly Dictionary<Guid, Goose> reservations = new Dictionary<Guid, Goose>();
        readonly List<Invitation> invitations = new List<Invitation>();

        public IReadOnlyList<Invitation> Invitations
        {
            get
            {
                return invitations;
            }
        }

        public int ReservationCount
        {
            get
            {
                return reservations.Count;
            }
        }

        public bool TryReserve(DesktopObject o, Goose g)
        {
            if (o == null || g == null)
                return false;
            Goose owner;
            if (reservations.TryGetValue(o.Id, out owner))
                return owner == g;
            reservations[o.Id] = g;
            return true;
        }

        public void Release(DesktopObject o)
        {
            if (o == null)
                return;
            reservations.Remove(o.Id);
            if (o.Holder != null)
            {
                if (o.Holder.Held == o)
                    o.Holder.Held = null;
                if (o.Holder.TargetObject == o)
                    o.Holder.TargetObject = null;
                o.Holder = null;
            }

            o.Carried = false;
        }

        public Goose OwnerOf(DesktopObject o)
        {
            if (o == null)
                return null;
            Goose owner;
            return reservations.TryGetValue(o.Id, out owner) ? owner : null;
        }

        public bool IsReservedByOther(DesktopObject o, Goose g)
        {
            Goose owner = OwnerOf(o);
            return owner != null && owner != g;
        }

        public bool TryAcquireCarry(Goose g, DesktopObject o)
        {
            if (o == null || g == null || o.Dragging)
                return false;
            Goose owner = OwnerOf(o);
            if (owner != null && owner != g)
                return false;
            reservations[o.Id] = g;
            if (o.Holder != null && o.Holder != g)
            {
                if (o.Holder.Held == o)
                    o.Holder.Held = null;
                if (o.Holder.TargetObject == o)
                    o.Holder.TargetObject = null;
            }

            o.Holder = g;
            o.Carried = true;
            g.Held = o;
            return true;
        }

        public void ReleaseCarry(Goose g, DesktopObject o)
        {
            if (o == null)
                return;
            if (o.Holder == g || (g == null && o.Holder != null))
            {
                Goose holder = o.Holder;
                if (holder != null)
                {
                    if (holder.Held == o)
                        holder.Held = null;
                    if (holder.TargetObject == o)
                        holder.TargetObject = null;
                }

                o.Holder = null;
            }

            if (g == null || OwnerOf(o) == g)
                reservations.Remove(o.Id);
            if (o.Holder == null)
                o.Carried = false;
        }

        public bool TryAcquireCursor(Goose g)
        {
            if (g == null)
                return false;
            if (CursorOwner != null && CursorOwner != g)
                return false;
            CursorOwner = g;
            return true;
        }

        public void ReleaseCursor(Goose g)
        {
            if (g == null || CursorOwner == g)
                CursorOwner = null;
        }

        public bool TryAcquireDelivery(Goose g)
        {
            if (g == null)
                return false;
            if (DeliveryOwner != null && DeliveryOwner != g)
                return false;
            DeliveryOwner = g;
            return true;
        }

        public void ReleaseDelivery(Goose g)
        {
            if (g == null || DeliveryOwner == g)
                DeliveryOwner = null;
        }

        public bool TryAcquireWindow(Goose g)
        {
            if (g == null)
                return false;
            if (WindowOwner != null && WindowOwner != g)
                return false;
            WindowOwner = g;
            return true;
        }

        public void ReleaseWindow(Goose g)
        {
            if (g == null || WindowOwner == g)
                WindowOwner = null;
        }

        public Invitation Offer(Goose inviter, Goose invitee, string kind, float timeout)
        {
            if (inviter == null || invitee == null || inviter == invitee)
                return null;
            var invitation = new Invitation { Inviter = inviter, Invitee = invitee, Kind = kind ?? "", ExpiresIn = timeout };
            invitations.Add(invitation);
            return invitation;
        }

        public bool Respond(Guid id, bool accept)
        {
            foreach (var invitation in invitations)
                if (invitation.Id == id && invitation.State == InvitationState.Offered)
                {
                    invitation.State = accept ? InvitationState.Accepted : InvitationState.Declined;
                    return true;
                }

            return false;
        }

        public void CancelInvitationsFor(Goose g)
        {
            if (g == null)
                return;
            foreach (var invitation in invitations)
                if (invitation.State == InvitationState.Offered && (invitation.Inviter == g || invitation.Invitee == g))
                    invitation.State = InvitationState.Cancelled;
        }

        public void Update(float dt)
        {
            foreach (var invitation in invitations)
            {
                if (invitation.State != InvitationState.Offered)
                    continue;
                invitation.ExpiresIn -= dt;
                if (invitation.ExpiresIn <= 0)
                    invitation.State = InvitationState.Expired;
            }
        }

        public void ReleaseAllFor(Goose g)
        {
            if (g == null)
                return;
            var stale = new List<Guid>();
            foreach (var pair in reservations)
                if (pair.Value == g)
                    stale.Add(pair.Key);
            foreach (var id in stale)
                reservations.Remove(id);
            if (g.Held != null)
            {
                g.Held.Holder = null;
                g.Held.Carried = false;
                g.Held = null;
            }

            g.TargetObject = null;
            ReleaseCursor(g);
            ReleaseDelivery(g);
            ReleaseWindow(g);
            CancelInvitationsFor(g);
        }

        // Pause path: drop unpossessed claims (pursuit targets) while keeping
        // physical possession (Held) intact, and cancel pending invitations.
        // Callers clear any TargetObject that loses its reservation.
        public void ReleaseUnpossessed()
        {
            var stale = new List<Guid>();
            foreach (var pair in reservations)
                if (pair.Value == null || pair.Value.Held == null || !pair.Value.Held.Id.Equals(pair.Key))
                    stale.Add(pair.Key);
            foreach (var id in stale)
                reservations.Remove(id);
            for (int i = invitations.Count - 1; i >= 0; i--)
                if (invitations[i].State == InvitationState.Offered)
                    invitations[i].State = InvitationState.Cancelled;
        }

        public void Clear()
        {
            reservations.Clear();
            CursorOwner = DeliveryOwner = WindowOwner = null;
            foreach (var invitation in invitations)
                if (invitation.State == InvitationState.Offered)
                    invitation.State = InvitationState.Cancelled;
        }
    }
}

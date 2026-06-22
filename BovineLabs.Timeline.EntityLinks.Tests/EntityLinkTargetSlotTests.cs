using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.EntityLinks.Data;
using NUnit.Framework;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Tests
{
    public class EntityLinkTargetSlotTests
    {
        private static Targets Seed()
        {
            return new Targets
            {
                Owner = new Entity { Index = 1, Version = 1 },
                Source = new Entity { Index = 2, Version = 1 },
                Target = new Entity { Index = 3, Version = 1 },
                Custom = new Entity { Index = 4, Version = 1 }
            };
        }

        [Test]
        public void WriteOwner_SetsOnlyOwner_ReturnsTrue()
        {
            var targets = Seed();
            var value = new Entity { Index = 9, Version = 1 };

            Assert.IsTrue(EntityLinkTargetSlot.TrySet(ref targets, Target.Owner, value));

            Assert.AreEqual(value, targets.Owner);
            Assert.AreEqual(new Entity { Index = 2, Version = 1 }, targets.Source);
            Assert.AreEqual(new Entity { Index = 3, Version = 1 }, targets.Target);
            Assert.AreEqual(new Entity { Index = 4, Version = 1 }, targets.Custom);
        }

        [Test]
        public void WriteSource_SetsOnlySource_ReturnsTrue()
        {
            var targets = Seed();
            var value = new Entity { Index = 9, Version = 1 };

            Assert.IsTrue(EntityLinkTargetSlot.TrySet(ref targets, Target.Source, value));

            Assert.AreEqual(new Entity { Index = 1, Version = 1 }, targets.Owner);
            Assert.AreEqual(value, targets.Source);
            Assert.AreEqual(new Entity { Index = 3, Version = 1 }, targets.Target);
            Assert.AreEqual(new Entity { Index = 4, Version = 1 }, targets.Custom);
        }

        [Test]
        public void WriteTarget_SetsOnlyTarget_ReturnsTrue()
        {
            var targets = Seed();
            var value = new Entity { Index = 9, Version = 1 };

            Assert.IsTrue(EntityLinkTargetSlot.TrySet(ref targets, Target.Target, value));

            Assert.AreEqual(new Entity { Index = 1, Version = 1 }, targets.Owner);
            Assert.AreEqual(new Entity { Index = 2, Version = 1 }, targets.Source);
            Assert.AreEqual(value, targets.Target);
            Assert.AreEqual(new Entity { Index = 4, Version = 1 }, targets.Custom);
        }

        [Test]
        public void WriteCustom_SetsOnlyCustom_ReturnsTrue()
        {
            var targets = Seed();
            var value = new Entity { Index = 9, Version = 1 };

            Assert.IsTrue(EntityLinkTargetSlot.TrySet(ref targets, Target.Custom, value));

            Assert.AreEqual(new Entity { Index = 1, Version = 1 }, targets.Owner);
            Assert.AreEqual(new Entity { Index = 2, Version = 1 }, targets.Source);
            Assert.AreEqual(new Entity { Index = 3, Version = 1 }, targets.Target);
            Assert.AreEqual(value, targets.Custom);
        }

        [Test]
        public void WriteNone_ReturnsFalse_NoMutation()
        {
            var targets = Seed();
            var value = new Entity { Index = 9, Version = 1 };

            Assert.IsFalse(EntityLinkTargetSlot.TrySet(ref targets, Target.None, value));

            AssertUnchanged(targets);
        }

        [Test]
        public void WriteSelf_ReturnsFalse_NoMutation()
        {
            var targets = Seed();
            var value = new Entity { Index = 9, Version = 1 };

            Assert.IsFalse(EntityLinkTargetSlot.TrySet(ref targets, Target.Self, value));

            AssertUnchanged(targets);
        }

        [Test]
        public void WriteUnknownValue_ReturnsFalse_NoMutation()
        {
            var targets = Seed();
            var value = new Entity { Index = 9, Version = 1 };

            Assert.IsFalse(EntityLinkTargetSlot.TrySet(ref targets, (Target)200, value));

            AssertUnchanged(targets);
        }

        private static void AssertUnchanged(Targets targets)
        {
            Assert.AreEqual(new Entity { Index = 1, Version = 1 }, targets.Owner);
            Assert.AreEqual(new Entity { Index = 2, Version = 1 }, targets.Source);
            Assert.AreEqual(new Entity { Index = 3, Version = 1 }, targets.Target);
            Assert.AreEqual(new Entity { Index = 4, Version = 1 }, targets.Custom);
        }
    }
}

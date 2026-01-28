using Verse;

namespace Simple_Trans
{
    /// <summary>
    /// CompProperties for CompEmbryoParentInfo
    /// </summary>
    public class CompProperties_EmbryoParentInfo : CompProperties
    {
        public CompProperties_EmbryoParentInfo()
        {
            compClass = typeof(CompEmbryoParentInfo);
        }
    }

    /// <summary>
    /// ThingComp that stores explicit parent references for embryos.
    /// This tracks who was the carrier (ovum provider) and sirer (fertilizer)
    /// at the time of embryo creation, independent of their current gender or capabilities.
    /// </summary>
    public class CompEmbryoParentInfo : ThingComp
    {
        /// <summary>
        /// The pawn who provided the ovum (carrier role / "mother")
        /// </summary>
        public Pawn carrier;

        /// <summary>
        /// The pawn who fertilized the ovum (sirer role / "father")
        /// </summary>
        public Pawn sirer;

        /// <summary>
        /// Whether this comp has been populated with parent info
        /// </summary>
        public bool HasParentInfo => carrier != null || sirer != null;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref carrier, "carrier");
            Scribe_References.Look(ref sirer, "sirer");
        }
    }
}

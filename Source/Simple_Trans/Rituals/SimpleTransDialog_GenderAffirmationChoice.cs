using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Simple_Trans
{
    public enum GenderIdentity
    {
        Cisgender,
        Transgender
    }

    public class SimpleTransDialog_GenderAffirmationChoice : Window
    {
        private Pawn celebrant;
        private LordJob_Ritual ritual;
        private string firstName;
        private string nickName;
        private string lastName;
        private Gender selectedGender;
        private GenderIdentity selectedIdentity;
        private bool nameChanged = false;

        public override Vector2 InitialSize => new Vector2(500f, 650f);

        public SimpleTransDialog_GenderAffirmationChoice(Pawn celebrant, LordJob_Ritual ritual)
        {
            this.celebrant = celebrant;
            this.ritual = ritual;
            this.selectedGender = celebrant.gender;
            this.selectedIdentity = GetCurrentIdentity(celebrant);

            // Initialize name fields based on current name type
            if (celebrant.Name is NameSingle nameSingle)
            {
                this.firstName = nameSingle.Name;
                this.nickName = "";
                this.lastName = "";
            }
            else if (celebrant.Name is NameTriple nameTriple)
            {
                this.firstName = nameTriple.First;
                this.nickName = nameTriple.Nick.Equals(nameTriple.First) ? "" : nameTriple.Nick;
                this.lastName = nameTriple.Last;
            }
            else
            {
                this.firstName = "";
                this.nickName = "";
                this.lastName = "";
            }

            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = false;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            var titleRect = new Rect(0f, 0f, inRect.width, 35f);
            string pawnName = celebrant.Name?.ToStringShort ?? "SimpleTrans.Ritual.Someone".Translate().ToString();
            Widgets.Label(titleRect, "SimpleTrans.Ritual.Title".Translate(pawnName));

            Text.Font = GameFont.Small;
            float curY = 50f;

            // Description
            var descRect = new Rect(0f, curY, inRect.width, 60f);
            Widgets.Label(descRect, "SimpleTrans.Ritual.Description".Translate(pawnName));
            curY += 70f;

            // Current name display
            var currentNameRect = new Rect(0f, curY, inRect.width, 25f);
            string currentName = celebrant.Name?.ToStringShort ?? "SimpleTrans.Ritual.Unknown".Translate().ToString();
            Widgets.Label(currentNameRect, "SimpleTrans.Ritual.CurrentName".Translate(currentName));
            curY += 30f;

            // First name input
            var firstNameLabelRect = new Rect(0f, curY, inRect.width, 25f);
            Widgets.Label(firstNameLabelRect, "SimpleTrans.Ritual.FirstName".Translate());
            curY += 25f;
            var firstNameRect = new Rect(0f, curY, inRect.width, 30f);
            GUI.SetNextControlName("FirstNameField");
            string inputFirstName = Widgets.TextField(firstNameRect, firstName);
            if (inputFirstName != firstName)
            {
                firstName = inputFirstName;
                nameChanged = true;
            }
            curY += 35f;

            // Nickname input
            var nickNameLabelRect = new Rect(0f, curY, inRect.width, 25f);
            Widgets.Label(nickNameLabelRect, "SimpleTrans.Ritual.NicknameOptional".Translate());
            curY += 25f;
            var nickNameRect = new Rect(0f, curY, inRect.width, 30f);
            GUI.SetNextControlName("NickNameField");
            string inputNickName = Widgets.TextField(nickNameRect, nickName);
            if (inputNickName != nickName)
            {
                nickName = inputNickName;
                nameChanged = true;
            }
            curY += 35f;

            // Last name input
            var lastNameLabelRect = new Rect(0f, curY, inRect.width, 25f);
            Widgets.Label(lastNameLabelRect, "SimpleTrans.Ritual.LastName".Translate());
            curY += 25f;
            var lastNameRect = new Rect(0f, curY, inRect.width, 30f);
            GUI.SetNextControlName("LastNameField");
            string inputLastName = Widgets.TextField(lastNameRect, lastName);
            if (inputLastName != lastName)
            {
                lastName = inputLastName;
                nameChanged = true;
            }
            curY += 40f;

            // Gender selection
            var genderLabelRect = new Rect(0f, curY, inRect.width, 25f);
            Widgets.Label(genderLabelRect, "SimpleTrans.Ritual.GenderLabel".Translate());
            curY += 30f;

            var genderRect = new Rect(0f, curY, inRect.width, 30f);
            if (Widgets.ButtonText(new Rect(0f, curY, 100f, 30f), GetGenderDisplayName(selectedGender)))
            {
                var genderOptions = new List<FloatMenuOption>
                {
                    new FloatMenuOption(Gender.Male.GetLabel().CapitalizeFirst(), () => selectedGender = Gender.Male),
                    new FloatMenuOption(Gender.Female.GetLabel().CapitalizeFirst(), () => selectedGender = Gender.Female)
                };

                // Add Enby option if NonBinary Gender mod is detected
                if (SimpleTrans.NBGenderActive)
                {
                    genderOptions.Add(new FloatMenuOption("SimpleTrans.Gender.Enby".Translate(), () => selectedGender = (Gender)3));
                }

                Find.WindowStack.Add(new FloatMenu(genderOptions));
            }
            curY += 40f;

            // Identity selection
            var identityLabelRect = new Rect(0f, curY, inRect.width, 25f);
            Widgets.Label(identityLabelRect, "SimpleTrans.Ritual.IdentityLabel".Translate());
            curY += 30f;

            if (Widgets.ButtonText(new Rect(0f, curY, 120f, 30f), GetIdentityDisplayName(selectedIdentity)))
            {
                var identityOptions = new List<FloatMenuOption>
                {
                    new FloatMenuOption("SimpleTrans.Identity.Cisgender".Translate(), () => selectedIdentity = GenderIdentity.Cisgender),
                    new FloatMenuOption("SimpleTrans.Identity.Transgender".Translate(), () => selectedIdentity = GenderIdentity.Transgender)
                };

                Find.WindowStack.Add(new FloatMenu(identityOptions));
            }
            curY += 50f;

            // Note about reproductive capabilities
            var noteRect = new Rect(0f, curY, inRect.width, 40f);
            Widgets.Label(noteRect, "SimpleTrans.Ritual.Note".Translate());
            curY += 50f;

            // Buttons
            var buttonWidth = (inRect.width - 20f) / 2f;

            if (Widgets.ButtonText(new Rect(0f, curY, buttonWidth, 35f), "CancelButton".Translate()))
            {
                Close();
            }

            if (Widgets.ButtonText(new Rect(buttonWidth + 20f, curY, buttonWidth, 35f), "Confirm".Translate()))
            {
                ApplyChanges();
                Close();
            }
        }

        private GenderIdentity GetCurrentIdentity(Pawn pawn)
        {
            if (pawn.health.hediffSet.HasHediff(SimpleTransHediffs.transDef))
                return GenderIdentity.Transgender;
            else if (pawn.health.hediffSet.HasHediff(SimpleTransHediffs.cisDef))
                return GenderIdentity.Cisgender;
            else
                return GenderIdentity.Cisgender; // Default
        }

        private string GetGenderDisplayName(Gender gender)
        {
            // Use RimWorld's built-in gender labels for Male/Female, custom for Enby
            if ((int)gender == 3) // Enby from NonBinary Gender mod
            {
                return "SimpleTrans.Gender.Enby".Translate();
            }
            return gender.GetLabel().CapitalizeFirst();
        }

        private string GetIdentityDisplayName(GenderIdentity identity)
        {
            return identity == GenderIdentity.Cisgender
                ? "SimpleTrans.Identity.Cisgender".Translate()
                : "SimpleTrans.Identity.Transgender".Translate();
        }

        private void ApplyChanges()
        {
            bool anyChanges = false;
            string oldName = celebrant.Name?.ToStringFull ?? "unknown";

            // Apply name changes if different
            if (nameChanged)
            {
                // Ensure first name is not empty
                if (firstName.NullOrEmpty())
                {
                    firstName = celebrant.Name is NameTriple nameTriple ? nameTriple.First : ((NameSingle)celebrant.Name).Name;
                }

                // Determine if this should be a NameTriple (has last name) or NameSingle
                bool isNameTriple = !lastName.NullOrEmpty();
                if (isNameTriple && nickName.NullOrEmpty())
                {
                    nickName = firstName;
                }

                // Apply the new name
                if (isNameTriple)
                {
                    celebrant.Name = new NameTriple(firstName, nickName, lastName);
                }
                else
                {
                    celebrant.Name = new NameSingle(firstName);
                }

                anyChanges = true;

                SimpleTransDebug.Log($"Gender affirmation ritual: Changed name from '{oldName}' to '{celebrant.Name?.ToStringFull ?? "unknown"}'", 1);
            }

            // Apply gender change if different
            if (selectedGender != celebrant.gender)
            {
                celebrant.gender = selectedGender;
                anyChanges = true;

                SimpleTransDebug.Log($"Gender affirmation ritual: Changed {celebrant.Name?.ToStringShort ?? "unknown"}'s gender to {selectedGender}", 1);
            }

            // Apply identity change if different
            GenderIdentity currentIdentity = GetCurrentIdentity(celebrant);
            if (selectedIdentity != currentIdentity)
            {
                // Remove current identity hediffs only (not reproductive capabilities)
                GenderAssignment.ClearGender(celebrant, clearIdentity: true, clearCapabilities: false);

                // Add new identity hediff
                if (selectedIdentity == GenderIdentity.Transgender)
                {
                    GenderAssignment.SetTrans(celebrant);
                }
                else
                {
                    GenderAssignment.SetCis(celebrant);
                }

                anyChanges = true;

                SimpleTransDebug.Log($"Gender affirmation ritual: Changed {celebrant.Name?.ToStringShort ?? "unknown"}'s identity from {currentIdentity} to {selectedIdentity}", 1);
            }

            if (anyChanges)
            {
                // Create a detailed message about what changed
                var changeDetails = new List<string>();
                if (nameChanged)
                {
                    if (oldName != celebrant.Name?.ToStringFull)
                    {
                        changeDetails.Add("SimpleTrans.Ritual.ChangedNameTo".Translate(celebrant.Name?.ToStringFull ?? "").ToString());
                    }
                }

                string fullName = celebrant.Name?.ToStringFull ?? "SimpleTrans.Ritual.Someone".Translate().ToString();
                string message;
                if (changeDetails.Any())
                {
                    message = "SimpleTrans.Ritual.AffirmedIdentityWithChanges".Translate(fullName, string.Join(" and ", changeDetails));
                }
                else
                {
                    message = "SimpleTrans.Ritual.AffirmedIdentity".Translate(fullName);
                }

                // Send message about the changes
                Messages.Message(message, celebrant, MessageTypeDefOf.PositiveEvent);

                // Refresh any cached data
                celebrant.Drawer?.renderer?.SetAllGraphicsDirty();
                if (celebrant.apparel != null)
                {
                    PortraitsCache.SetDirty(celebrant);
                }
            }
        }

        public override void OnAcceptKeyPressed()
        {
            ApplyChanges();
            Close();
        }

        public override void OnCancelKeyPressed()
        {
            Close();
        }
    }
}

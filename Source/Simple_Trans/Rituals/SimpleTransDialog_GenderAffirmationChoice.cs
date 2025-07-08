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
            Widgets.Label(titleRect, $"{celebrant.Name?.ToStringShort ?? "Someone"}'s Gender Affirmation");

            Text.Font = GameFont.Small;
            float curY = 50f;

            // Description
            var descRect = new Rect(0f, curY, inRect.width, 60f);
            Widgets.Label(descRect, "Choose the name and gender identity that " + celebrant.Name?.ToStringShort + " wishes to be known by:");
            curY += 70f;

            // Current name display
            var currentNameRect = new Rect(0f, curY, inRect.width, 25f);
            Widgets.Label(currentNameRect, "Current name: " + (celebrant.Name?.ToStringShort ?? "Unknown"));
            curY += 30f;

            // First name input
            var firstNameLabelRect = new Rect(0f, curY, inRect.width, 25f);
            Widgets.Label(firstNameLabelRect, "First name:");
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
            Widgets.Label(nickNameLabelRect, "Nickname (optional):");
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
            Widgets.Label(lastNameLabelRect, "Last name:");
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
            Widgets.Label(genderLabelRect, "Gender identity:");
            curY += 30f;

            var genderRect = new Rect(0f, curY, inRect.width, 30f);
            if (Widgets.ButtonText(new Rect(0f, curY, 100f, 30f), GetGenderDisplayName(selectedGender)))
            {
                var genderOptions = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Male", () => selectedGender = Gender.Male),
                    new FloatMenuOption("Female", () => selectedGender = Gender.Female)
                };

                // Add Enby option if NonBinary Gender mod is detected
                if (SimpleTrans.NBGenderActive)
                {
                    genderOptions.Add(new FloatMenuOption("Enby", () => selectedGender = (Gender)3));
                }

                Find.WindowStack.Add(new FloatMenu(genderOptions));
            }
            curY += 40f;

            // Identity selection
            var identityLabelRect = new Rect(0f, curY, inRect.width, 25f);
            Widgets.Label(identityLabelRect, "Gender identity (cis/trans):");
            curY += 30f;

            if (Widgets.ButtonText(new Rect(0f, curY, 120f, 30f), selectedIdentity.ToString()))
            {
                var identityOptions = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Cisgender", () => selectedIdentity = GenderIdentity.Cisgender),
                    new FloatMenuOption("Transgender", () => selectedIdentity = GenderIdentity.Transgender)
                };

                Find.WindowStack.Add(new FloatMenu(identityOptions));
            }
            curY += 50f;

            // Note about reproductive capabilities
            var noteRect = new Rect(0f, curY, inRect.width, 40f);
            Widgets.Label(noteRect, "Note: This only changes name and gender identity.\nReproductive capabilities are unchanged.");
            curY += 50f;

            // Buttons
            var buttonWidth = (inRect.width - 20f) / 2f;

            if (Widgets.ButtonText(new Rect(0f, curY, buttonWidth, 35f), "Cancel"))
            {
                Close();
            }

            if (Widgets.ButtonText(new Rect(buttonWidth + 20f, curY, buttonWidth, 35f), "Confirm"))
            {
                ApplyChanges();
                Close();
            }
        }

        private GenderIdentity GetCurrentIdentity(Pawn pawn)
        {
            if (pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.transDef))
                return GenderIdentity.Transgender;
            else if (pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.cisDef))
                return GenderIdentity.Cisgender;
            else
                return GenderIdentity.Cisgender; // Default
        }

        private string GetGenderDisplayName(Gender gender)
        {
            // Always use Enum.GetName to hit any harmony patches (like NonBinary Gender mod)
            return System.Enum.GetName(typeof(Gender), gender) ?? gender.ToString();
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

                if (SimpleTrans.debugMode)
                {
                    Log.Message($"[Simple Trans DEBUG] Changed name from '{oldName}' to '{celebrant.Name?.ToStringFull ?? "unknown"}'");
                }
            }

            // Apply gender change if different
            if (selectedGender != celebrant.gender)
            {
                celebrant.gender = selectedGender;
                anyChanges = true;

                if (SimpleTrans.debugMode)
                {
                    Log.Message($"[Simple Trans DEBUG] Changed {celebrant.Name?.ToStringShort ?? "unknown"}'s gender to {selectedGender}");
                }
            }

            // Apply identity change if different
            GenderIdentity currentIdentity = GetCurrentIdentity(celebrant);
            if (selectedIdentity != currentIdentity)
            {
                // Remove current identity hediffs only (not reproductive capabilities)
                SimpleTransPregnancyUtility.ClearGender(celebrant, clearIdentity: true, clearCapabilities: false);

                // Add new identity hediff
                if (selectedIdentity == GenderIdentity.Transgender)
                {
                    SimpleTransPregnancyUtility.SetTrans(celebrant);
                }
                else
                {
                    SimpleTransPregnancyUtility.SetCis(celebrant);
                }

                anyChanges = true;

                if (SimpleTrans.debugMode)
                {
                    Log.Message($"[Simple Trans DEBUG] Changed {celebrant.Name?.ToStringShort ?? "unknown"}'s identity to {selectedIdentity}");
                }
            }

            if (anyChanges)
            {
                // Create a detailed message about what changed
                var changeDetails = new List<string>();
                if (nameChanged)
                {
                    if (oldName != celebrant.Name?.ToStringFull)
                    {
                        changeDetails.Add($"changed their name to {celebrant.Name?.ToStringFull}");
                    }
                }

                string message;
                if (changeDetails.Any())
                {
                    message = $"{celebrant.Name?.ToStringFull ?? "Someone"} has affirmed their identity and {string.Join(" and ", changeDetails)} during the celebration!";
                }
                else
                {
                    message = $"{celebrant.Name?.ToStringFull ?? "Someone"} has affirmed their identity during the celebration!";
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

using System;

namespace Alsappan.Domain.Residents;

[Flags]
public enum ResidentPrivacyOptions
{
  None = 0,
  ContactData = 1,
  IdentificationData = 2,
  EmergencyContact = 4,
  Notes = 8
}

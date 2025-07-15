using ERezeptExtractor.Models;

namespace ERezeptExtractor.Validation
{
    /// <summary>
    /// Validation helper for eRezept data
    /// </summary>
    public static class ERezeptValidator
    {
        /// <summary>
        /// Validates the extracted eRezept data
        /// </summary>
        /// <param name="data">The data to validate</param>
        /// <returns>List of validation errors</returns>
        public static List<string> Validate(ERezeptData data)
        {
            var errors = new List<string>();

            if (data == null)
            {
                errors.Add("ERezeptData is null");
                return errors;
            }

            // Validate Bundle information
            if (string.IsNullOrWhiteSpace(data.BundleId))
                errors.Add("Bundle ID is missing");

            if (string.IsNullOrWhiteSpace(data.PrescriptionId))
                errors.Add("Prescription ID is missing");

            if (data.Timestamp == default)
                errors.Add("Timestamp is missing or invalid");

            // Validate Pharmacy information
            ValidatePharmacy(data.Pharmacy, errors);

            // Validate Medication Dispense information
            ValidateMedicationDispense(data.MedicationDispense, errors);

            // Validate Invoice information
            ValidateInvoice(data.Invoice, errors);

            return errors;
        }

        private static void ValidatePharmacy(PharmacyInfo pharmacy, List<string> errors)
        {
            if (pharmacy == null)
            {
                errors.Add("Pharmacy information is missing");
                return;
            }

            if (string.IsNullOrWhiteSpace(pharmacy.Id))
                errors.Add("Pharmacy ID is missing");

            if (string.IsNullOrWhiteSpace(pharmacy.IK_Number))
                errors.Add("Pharmacy IK number is missing");

            if (string.IsNullOrWhiteSpace(pharmacy.Name))
                errors.Add("Pharmacy name is missing");

            ValidateAddress(pharmacy.Address, errors, "Pharmacy");
        }

        private static void ValidateAddress(AddressInfo address, List<string> errors, string context)
        {
            if (address == null)
            {
                errors.Add($"{context} address is missing");
                return;
            }

            if (string.IsNullOrWhiteSpace(address.Street))
                errors.Add($"{context} street is missing");

            if (string.IsNullOrWhiteSpace(address.PostalCode))
                errors.Add($"{context} postal code is missing");

            if (string.IsNullOrWhiteSpace(address.City))
                errors.Add($"{context} city is missing");
        }

        private static void ValidateMedicationDispense(MedicationDispenseInfo medicationDispense, List<string> errors)
        {
            if (medicationDispense == null)
            {
                errors.Add("Medication dispense information is missing");
                return;
            }

            if (string.IsNullOrWhiteSpace(medicationDispense.Id))
                errors.Add("Medication dispense ID is missing");

            if (string.IsNullOrWhiteSpace(medicationDispense.Status))
                errors.Add("Medication dispense status is missing");
        }

        private static void ValidateInvoice(InvoiceInfo invoice, List<string> errors)
        {
            if (invoice == null)
            {
                errors.Add("Invoice information is missing");
                return;
            }

            if (string.IsNullOrWhiteSpace(invoice.Id))
                errors.Add("Invoice ID is missing");

            if (string.IsNullOrWhiteSpace(invoice.Status))
                errors.Add("Invoice status is missing");

            if (invoice.TotalGross <= 0)
                errors.Add("Invoice total gross amount must be greater than 0");

            if (invoice.LineItems == null || !invoice.LineItems.Any())
                errors.Add("Invoice must contain at least one line item");

            ValidateLineItems(invoice.LineItems, errors);
        }

        private static void ValidateLineItems(List<LineItemInfo> lineItems, List<string> errors)
        {
            for (int i = 0; i < lineItems.Count; i++)
            {
                var lineItem = lineItems[i];
                var prefix = $"Line item {i + 1}";

                if (lineItem.Sequence <= 0)
                    errors.Add($"{prefix}: Sequence must be greater than 0");

                if (string.IsNullOrWhiteSpace(lineItem.PZN))
                    errors.Add($"{prefix}: PZN is missing");

                if (lineItem.Amount <= 0)
                    errors.Add($"{prefix}: Amount must be greater than 0");

                if (string.IsNullOrWhiteSpace(lineItem.Currency))
                    errors.Add($"{prefix}: Currency is missing");
            }
        }

        /// <summary>
        /// Checks if a PZN (Pharmazentralnummer) has a valid format
        /// </summary>
        /// <param name="pzn">The PZN to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidPZN(string pzn)
        {
            if (string.IsNullOrWhiteSpace(pzn))
                return false;

            // PZN should be 8 digits
            if (pzn.Length != 8)
                return false;

            return pzn.All(char.IsDigit);
        }

        /// <summary>
        /// Checks if an IK number has a valid format
        /// </summary>
        /// <param name="ikNumber">The IK number to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidIKNumber(string ikNumber)
        {
            if (string.IsNullOrWhiteSpace(ikNumber))
                return false;

            // IK number should be 9 digits
            if (ikNumber.Length != 9)
                return false;

            return ikNumber.All(char.IsDigit);
        }
    }
}

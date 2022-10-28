using PassportPDF.Api;
using PassportPDF.Client;
using PassportPDF.Model;

namespace DocumentConversion
{

    public class DocumentConverter
    {
        static async Task Main(string[] args)
        {
            GlobalConfiguration.ApiKey = "YOUR-PASSPORT-CODE";

            PassportManagerApi apiManager = new();
            PassportPDFPassport passportData = await apiManager.PassportManagerGetPassportInfoAsync(GlobalConfiguration.ApiKey);

            if (passportData == null)
            {
                throw new ApiException("The Passport number given is invalid, please set a valid passport number and try again.");
            }
            else if (passportData.IsActive is false)
            {
                throw new ApiException("The Passport number given not active, please go to your PassportPDF dashboard and active your plan.");
            }

            string uri = "https://passportpdfapi.com/test/invoice_with_barcode.pdf";

            DocumentApi api = new();

            Console.WriteLine("Loading document into PassportPDF...");
            DocumentLoadResponse document = await api.DocumentLoadFromURIAsync(new LoadDocumentFromURIParameters(uri));
            Console.WriteLine("Document loaded.");

            PDFApi pdfApi = new();


            // Check if PDF/A conformance level is PDFA3b
            Console.WriteLine("Checking if conformance level of PDF/A file is PDF/A-3b ...");

            PdfValidatePDFAResponse pdfValidatePdfaResponse = await pdfApi.ValidatePDFAAsync(new PdfValidatePDFAParameters(document.FileId));
            pdfValidatePdfaResponse = await pdfApi.ValidatePDFAAsync(new PdfValidatePDFAParameters(document.FileId)
            {
                Conformance = PdfAValidationConformance.PDFA3b
            });

            if (pdfValidatePdfaResponse.Error is not null)
            {
                throw new ApiException(pdfValidatePdfaResponse.Error.ExtResultMessage);
            }
            else
            {
                if(pdfValidatePdfaResponse.Conformance == PdfAValidationConformance.PDFA3b)
                {
                    Console.WriteLine("Your PDF/A file has the right conformance level");
                }
                else
                {
                    Console.WriteLine("Your PDF/A file does NOT have the right conformance level. Running conversion process to have PDF/A-3b conformance level ...");

                    try
                    {
                        // Convert PDF/A to your chosen conformance level : PDFA3b
                        PdfConvertToPDFAResponse pdfConvertResponse = await pdfApi.ConvertToPDFAAsync(new PdfConvertToPDFAParameters(document.FileId)
                        {
                            Conformance = PdfAConformance.PDFA3b
                        });

                        if (pdfConvertResponse.Error is not null)
                        {
                            throw new ApiException(pdfConvertResponse.Error.ExtResultMessage);
                        }
                        else
                        {
                            Console.WriteLine("Conversion process finished successfully. Downloading the new PDF/A file ...");

                            // Download file with PDF/A-3b conformance level

                            PdfSaveDocumentResponse saveDocResponse = await pdfApi.SaveDocumentAsync(new PdfSaveDocumentParameters(document.FileId));

                            string savePath = Path.Join(Directory.GetCurrentDirectory(), "pdfa3b_file.pdf");

                            File.WriteAllBytes(savePath, saveDocResponse.Data);

                            Console.WriteLine("Done downloading PDF/A file. Document has been saved in : {0}", savePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Could not convert or download new PDF/A file! Issue : {0}", ex.Message);
                    }
                }
            }
        }
    }
}



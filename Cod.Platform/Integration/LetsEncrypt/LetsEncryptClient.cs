using System.Collections.Generic;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;

namespace Cod.Platform
{
    internal class LetsEncryptClient : ICertificateAuthority
    {
        public async Task<SSLCertificateApplication> ApplySSLCertificateAsync(
            string domain,
            string email,
            string password,
            string country,
            string state = null,
            string city = null,
            string organization = null,
            string unit = null)
        {
            var acme = new AcmeContext(WellKnownServers.LetsEncryptV2);
            var account = await acme.NewAccount(email, true);

            // Save the account key for later use
            //var pemKey = acme.AccountKey.ToPem();

            // Load the saved account key
            //var accountKey = KeyFactory.FromPem(pemKey);
            //var acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2, accountKey);
            //var account = await acme.Account();

            var wildDomain = $"*.{domain}";
            var order = await acme.NewOrder(new[] { domain, wildDomain });
            var authrizations = await order.Authorizations();
            var challenges = new List<IChallengeContext>();
            var requirements = new List<DNSRecord>();

            foreach (var authrization in authrizations)
            {
                var challenge = await authrization.Dns();
                var dnsTxt = acme.AccountKey.DnsTxt(challenge.Token);
                challenges.Add(challenge);
                requirements.Add(new DNSRecord
                {
                    Domain = domain,
                    Record = "_acme-challenge",
                    Type = DNSRecordType.TXT,
                    Value = dnsTxt,
                });
            }

            return new SSLCertificateApplication
            {
                Requirements = requirements,
                Result = async () =>
                {
                    var r = new SSLCertificateApplicationResult();
                    var success = true;
                    for (var i = 0; i < 500; i++)
                    {
                        success = true;
                        await Task.Delay(1000);

                        foreach (var challenge in challenges)
                        {
                            var cr = await challenge.Validate();
                            if (!cr.Status.HasValue || cr.Status.Value != ChallengeStatus.Valid)
                            {
                                success = false;
                                break;
                            }
                        }

                        if (success)
                        {
                            break;
                        }
                    }

                    if (success)
                    {
                        var privateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
                        var cert = await order.Generate(new CsrInfo
                        {
                            CountryName = country,
                            State = state,
                            Locality = city,
                            Organization = organization,
                            OrganizationUnit = unit,
                        }, privateKey);

                        r.PEMCert = cert.ToPem();
                        r.PEMKey = privateKey.ToPem();
                        var pfxBuilder = cert.ToPfx(privateKey);
                        r.PFX = pfxBuilder.Build(domain, password);
                    }

                    return r;
                },
            };
        }
    }
}

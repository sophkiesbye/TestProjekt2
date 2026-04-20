namespace ordination_test;

using Microsoft.EntityFrameworkCore;

using Service;
using Data;
using shared.Model;

[TestClass]
public class ServiceTest
{
    private DataService service;

    [TestInitialize]
    public void SetupBeforeEachTest()
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdinationContext>();
        optionsBuilder.UseInMemoryDatabase(databaseName: "test-database");
        var context = new OrdinationContext(optionsBuilder.Options);
        service = new DataService(context);
        service.SeedData();
    }

    [TestMethod]
    public void PatientsExist()
    {
        Assert.IsNotNull(service.GetPatienter());
    }

    [TestMethod]
    public void OpretDagligFast()
    {
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();

        Assert.AreEqual(1, service.GetDagligFaste().Count());

        service.OpretDagligFast(patient.PatientId, lm.LaegemiddelId,
            2, 2, 1, 0, DateTime.Now, DateTime.Now.AddDays(3));

        Assert.AreEqual(2, service.GetDagligFaste().Count());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void TestAtKodenSmiderEnException()
    {
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();

        Assert.AreEqual(1, service.GetDagligSkæve().Count());

        Dosis[] doser = new Dosis[]
        {
            new Dosis(DateTime.Now.Date.AddHours(8), 2),
            new Dosis(DateTime.Now.Date.AddHours(12), 1),
            new Dosis(DateTime.Now.Date.AddHours(18), 2)
        };

        service.OpretDagligSkaev(patient.PatientId, lm.LaegemiddelId,
            doser, DateTime.Now, DateTime.Now.AddDays(3));

        Assert.AreEqual(2, service.GetDagligSkæve().Count());
    }

    [TestMethod]
    public void OpretPN()
    {
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();

        Assert.AreEqual(4, service.GetPNs().Count());

        service.OpretPN(patient.PatientId, lm.LaegemiddelId,
            5, DateTime.Now, DateTime.Now.AddDays(7));
        
        Assert.AreEqual(5, service.GetPNs().Count());
    }

    [TestMethod]
    public void AnvendOrdination()
    {
        Patient patient = service.GetPatienter().First(); // henter den første patient fra systemet 
        Laegemiddel lm = service.GetLaegemidler().First(); // henter første lægemiddel
        
        PN pn = service.OpretPN(patient.PatientId, lm.LaegemiddelId, 
            5, DateTime.Now, DateTime.Now.AddDays(7)); // opretter en PN-ordination - 5 enheder som er gyldig i 7 dage
        
        Assert.AreEqual(0, pn.getAntalGangeGivet()); // Tjekker at den endnu ikke er anvendt
        
        Dato dato = new Dato { dato = DateTime.Now.AddDays(2) }; // opretter en dato 2 dage frem
        string resultat = service.AnvendOrdination(pn.OrdinationId, dato); // anvender den ordination på den dato
        
        Assert.AreEqual("Dosis givet", resultat); // successbesked
        
        PN opdateretPN = service.GetPNs().First(p => p.OrdinationId == pn.OrdinationId); // henter den opdateret PN
        
        Assert.AreEqual(1, opdateretPN.getAntalGangeGivet()); // Tjekker at den nu er givet en gang
    }
    
    [TestMethod]
    public void BeregnAnbefaletDosis()
    {
        Patient patient = service.GetPatienter().First(); 
        Laegemiddel lm = service.GetLaegemidler().First(l => l.navn == "Paracetamol"); 
        
        double anbefaletDosis = service.GetAnbefaletDosisPerDøgn(patient.PatientId, lm.LaegemiddelId); // Beregner dosis
        
        double forventet = patient.vaegt * lm.enhedPrKgPrDoegnNormal; // udregner den forventet dosis
        
        Assert.AreEqual(forventet, anbefaletDosis, 0.001); // sammenligner tolerance 
    }
    
    [TestMethod]
    public void TestDagligFastSamletDosis()
    {
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();
        
        DagligFast df = service.OpretDagligFast(patient.PatientId, lm.LaegemiddelId,
            2, 1, 1, 0, DateTime.Now, DateTime.Now.AddDays(3)); // opretter daglig fast ordination
        
        Assert.AreEqual(4, df.doegnDosis()); // Tjekker daglig dosis
        Assert.AreEqual(16, df.samletDosis()); // Tjekker den samlet dosis 
    }
    [TestMethod]
    public void TestPNDoegnDosis()
    {
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();
        
        PN pn = service.OpretPN(patient.PatientId, lm.LaegemiddelId,
            5, DateTime.Now, DateTime.Now.AddDays(10)); // opretter PN
        
        service.AnvendOrdination(pn.OrdinationId, new Dato { dato = DateTime.Now }); // anvend 1 gang
        service.AnvendOrdination(pn.OrdinationId, new Dato { dato = DateTime.Now.AddDays(2) }); // anvend 2 gang
        service.AnvendOrdination(pn.OrdinationId, new Dato { dato = DateTime.Now.AddDays(4) }); // anvend 3 gang
        
        PN opdateretPN = service.GetPNs().First(p => p.OrdinationId == pn.OrdinationId); // henter opdateret 
        
        Assert.AreEqual(3, opdateretPN.doegnDosis()); // tjekker antal gange brugt
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))] // forventet exception
    public void TestOpretDagligFastMedUgyldigeDatoer()
    {
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();
        
        service.OpretDagligFast(patient.PatientId, lm.LaegemiddelId,
            2, 1, 1, 0, DateTime.Now, DateTime.Now.AddDays(-5)); // slutdato før startdato som giver fejl
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void TestAnvendOrdinationUdenforPeriode()
    {
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();
        
        PN pn = service.OpretPN(patient.PatientId, lm.LaegemiddelId,
            5, DateTime.Now, DateTime.Now.AddDays(5)); // gyldig i 5 dage
        
        service.AnvendOrdination(pn.OrdinationId, new Dato { dato = DateTime.Now.AddDays(10) }); // udenfor ordination som giver fejl
    }
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void TestGetAnbefaletDosisSmiderEnException()
    {
        service.GetAnbefaletDosisPerDøgn(999999, 1); // patient findes ikke
    }
    [TestMethod]
    public void TestAnbefaletDosisForLetPatient()
    {
        Patient patient = service.GetPatienter().First();
        
        double originalVaegt = patient.vaegt; // gemmer den originale vægt
        
        patient.vaegt = 20; // sletter lav vægt
        
        Laegemiddel lm = service.GetLaegemidler().First(l => l.navn == "Paracetamol");
        
        double anbefaletDosis = service.GetAnbefaletDosisPerDøgn(patient.PatientId, lm.LaegemiddelId);
        
        Assert.AreEqual(20, anbefaletDosis, 0.001); // minimumsregel
        
        patient.vaegt = originalVaegt; // gendanner vægt
    }
    
    [TestMethod]
    public void TestAnbefaletDosisForTungPatient()
    {
        Patient patient = service.GetPatienter().First();
        
        double originalVaegt = patient.vaegt;
        
        patient.vaegt = 130; // sætter høj vægt
        
        Laegemiddel lm = service.GetLaegemidler().First(l => l.navn == "Paracetamol");
        
        double anbefaletDosis = service.GetAnbefaletDosisPerDøgn(patient.PatientId, lm.LaegemiddelId);
        
        Assert.AreEqual(260, anbefaletDosis, 0.001); // maksimumsregel
        
        patient.vaegt = originalVaegt; // gendanner vægten
    }
    [TestMethod]
    public void TestLaegemidlerExist()
    {
        List<Laegemiddel> laegemidler = service.GetLaegemidler(); // henter liste
        
        Assert.IsNotNull(laegemidler); // tjekker ikke null
        Assert.IsTrue(laegemidler.Count > 0); // tjekker der er mindst en 
    }

    [TestMethod]
    public void TestAnvendOrdinationPaaDagligFast()
    {
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();
        
        DagligFast df = service.OpretDagligFast(patient.PatientId, lm.LaegemiddelId,
            2, 1, 1, 0, DateTime.Now, DateTime.Now.AddDays(5)); // opretter daglig fast
        
        Dato dato = new Dato { dato = DateTime.Now.AddDays(2) };
        string resultat = service.AnvendOrdination(df.OrdinationId, dato); // forsøger at anvende
        
        Assert.AreEqual("Ordination er ikke en PN ordination", resultat); // forventer fejlbesked
    }
}
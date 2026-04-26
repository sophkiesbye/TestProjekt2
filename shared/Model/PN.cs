namespace shared.Model;

public class PN : Ordination {
	public double antalEnheder { get; set; }
    public List<Dato> dates { get; set; } = new List<Dato>();

    public PN (DateTime startDen, DateTime slutDen, double antalEnheder, Laegemiddel laegemiddel) : base(laegemiddel, startDen, slutDen) {
		this.antalEnheder = antalEnheder;
	}

    public PN() : base(null!, new DateTime(), new DateTime()) {
    }

    /// <summary>
    /// Registrerer at der er givet en dosis på dagen givesDen
    /// Returnerer true hvis givesDen er inden for ordinationens gyldighedsperiode og datoen huskes
    /// Returner false ellers og datoen givesDen ignoreres
    /// </summary>
    public bool givDosis(Dato givesDen) {
	    if (givesDen.dato >= startDen && givesDen.dato <= slutDen) { //tjekker om givesDen.dato er indenfor periode
		    dates.Add(givesDen); //givesDen tilføjes til dates
		    return true;
	    }
	    return false;
    }

    public override double doegnDosis() {
	    if (dates.Count == 0) { // tjekker først om nogen doser er givet
		    return 0;
	    }
        
	    DateTime førsteDato = dates.Min(d => d.dato); //finder første dosis med min
	    DateTime sidsteDato = dates.Max(d => d.dato); //finder sidste dosis med max
	    int antalDage = (sidsteDato - førsteDato).Days + 1; //beregner antal dage mellem første og sidste dosis
        
	    return (dates.Count * antalEnheder) / antalDage; //returnerer antal af doser givet pr dag
    }


    public override double samletDosis() {
        return dates.Count() * antalEnheder;
    }

    public int getAntalGangeGivet() {
        return dates.Count();
    }

	public override String getType() {
		return "PN";
	}
}

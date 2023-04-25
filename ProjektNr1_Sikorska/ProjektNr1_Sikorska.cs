using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace ProjektNr1_Sikorska
{
    public partial class PsProjektNr1_Sikorska : Form
    {
        // deklaracja tablicy aktywności zakładek formularza
        private readonly bool[] psStanTabPage = { true, true, false };
        bool psWybranoProdukt = false; // zmienna do sprawdzania, czy wybrano produkt przed płatnością
        float psZapłaconaKwota = 0; // zmienna zbiera kolejne płatności użytkownika
        string psCena; float psCenaFloat; // zmienne do obliczania ceny końcowej: cena kupowanego produktu
                                          // wzięta z nazwy produkty, później przekonwertowana na formę liczbową
        float psReszta = 0.0F; // do obliczenia reszty (całkowity koszt - zapłacona kwota)
        string psTxt = "zł";  // symbol waluty, domyślnie złoty
        int psIndexWybranychProduktów = 1; // licznik produktów zaczynający się/zerowany do 1, używany w tabeli listy zakupów
        float psSumaLosowania = 0; // suma pieniędzy wylosowanych przez program podczas startu, jeśli jest większa od reszty,
                                   // program anuluje tranzakcję psi zwraca pieniądze użytkownikowi
                                   // zmienne do losowania
        float psResztaDoWypłaty = 0;
        private const int psLiczbaMonet = 50;
        readonly int[] psWylosowaneNominały = new int[psLiczbaMonet];
        readonly int[] psPodliczenie = new int[8];
        // deklaracja psi utworzenie egzemplarzy kontrolek, które będą umieszczone na formularzu
        readonly Label psLblEtykietaDolnejGranicyPrzedziału = new Label();
        readonly Label psLblWypłacaneNominały = new Label();
        readonly TextBox psTxtDolnaGranicaPrzedziału = new TextBox();
        readonly TextBox psTxtGórnaGranicaPrzedziału = new TextBox();
        PictureBox psClickedPicture = new PictureBox(); // użyta w wyborze produktów przez użytkownika
        PictureBox psWybórMonety = new PictureBox();    // użyta w wyborze monet przez użytkownika
        // deklaracje stałych
        const ushort psMaxLicznośćNominałów = 100;
        readonly float[] psWartośćNominałów = { 200, 100, 50, 20, 10, 5, 2, 1, 0.5F, 0.2F, 0.1F, 0.05F, 0.02F, 0.01F };
        // deklaracja struktury (rekordu) opisującego element tablicy PsPojemnikNominałów
        struct PsNominały
        {
            public float PsWartość;
            public ushort PsLiczność;
        }

        // deklaracja zmiennej tablicowej (referencyjnej) pojemnikNominałów
        readonly PsNominały[] PsPojemnikNominałów;
        public PsProjektNr1_Sikorska()
        {
            InitializeComponent();
            // ustawienie w tablicy stanTabPage aktywności zakładek umieszczonych w formularzu
            psTcZakładki.SelectedIndex = 0;
            PsPojemnikNominałów = new PsNominały[psWartośćNominałów.Length];

        }
        private void ProjektNr1_Sikorska_Load(object sender, EventArgs e)
        {
            // przeprowadzenie losowania w celu ustalenia monetarnej zawartości automatu
            PsLosoweNominały();
            // dodanie pierwszego rzędu w liście zakupów
            psDgvListaProduktów.Rows.Add();
        }
        static Boolean PsCzyWypłataMożeByćZrealizowana(PsNominały[] PsPojemnikNominałów, float psKwotaDoWypłaty)
        {
            float psKapitałBankomatu = 0;
            for (int psi = 0; psi < PsPojemnikNominałów.Length; psi++)
                if (PsPojemnikNominałów[psi].PsLiczność > 0)
                    psKapitałBankomatu += (PsPojemnikNominałów[psi].PsLiczność * PsPojemnikNominałów[psi].PsWartość);
            // zwrócenie wyniku
            return psKapitałBankomatu >= psKwotaDoWypłaty;
        }
        // obsługa przycisków nawigujących między stronami
        private void PsBtnPrzejścieDoPulpitu_Click(object sender, EventArgs e)
        {
            int n = 0;
            PsPrzeskoczenieMiędzyStronami(n);
        }
        private void PsBtnPrzejścieDoAutomatu_Click(object sender, EventArgs e)
        {
            int n = 2;
            PsPrzeskoczenieMiędzyStronami(n);
        }
        private void PsBtnPrzejścieDoBankomatu_Click(object sender, EventArgs e)
        {
            int n = 1;
            PsPrzeskoczenieMiędzyStronami(n);
        }
        private void PsPrzeskoczenieMiędzyStronami(int n)
        {
            psStanTabPage[n] = true;
            psStanTabPage[(n + 1 + 3) % 3] = false;
            psStanTabPage[(n - 1 + 3) % 3] = false;
            TabPage[] tabPages = { psTpgPulpit, psTpgBankomat, psTpgAutomat };
            psTcZakładki.SelectedTab = tabPages[n];

            //if (n == 0)
            //{
            //    // zmiana stanu aktywności zakładki Pulpit
            //    psStanTabPage[0] = true;
            //    psStanTabPage[1] = false;
            //    psStanTabPage[2] = false;
            //    tcZakładki.SelectedTab = tabPage3;
            //}
            //else if (n == 1)
            //{
            //    // zmiana stanu aktywności zakładki Pulpit
            //    psStanTabPage[0] = false;
            //    // przejście do zakładki Bankomat
            //    psStanTabPage[1] = true;
            //    psStanTabPage[2] = false;
            //    tcZakładki.SelectedTab = tabPage2;
            //}
            //else if (n == 2)
            //{
            //    // zmiana stanu aktywności zakładki Pulpit
            //    psStanTabPage[0] = false;
            //    psStanTabPage[1] = false;
            //    psStanTabPage[2] = true;
            //    tcZakładki.SelectedTab = tabPage3;
            //}
        }
        private void PsTcZakładki_Selecting(object sender, TabControlCancelEventArgs e)
        {
            for (int psi = 0; psi < 3; psi++)
            {
                // sprawdzenie, czy zakładka jest aktywna
                if (e.TabPage == psTcZakładki.TabPages[psi])
                    if (psStanTabPage[psi])
                    {
                        e.Cancel = false; // zezwolenie na przejście do wybranej zakładki tabPageN
                        psTcZakładki.SelectedIndex = psi;
                    }
                    else
                        e.Cancel = true; // nie ma przejścia do wybranej zakładki tabPageN
            }

        }
        private void PsRdbUstawienieUżytkownika_CheckedChanged(object sender, EventArgs e)
        {

            // sprawdzenie, czy została wybrana waluta
            if (psCmbRodzajWaluty.SelectedIndex < 0)
            {
                psErrorProvider1.SetError(psCmbRodzajWaluty, "ERROR: musosz wybrać walutę!");
                return;
            }
            /* sprawdzenie, czy zdarzenie CheckedChanged zostało wywołane (wygenerowane) przez
             * metodę obsługi przycisku poleceń RESETUJ */
            if (psRdbUstawienieUżytkownika.Checked == false)
                // to nic nie robimy
                return;
            // sformatowanie kontrolek, które zamierzamy umieścić na formularzu
            psLblEtykietaDolnejGranicyPrzedziału.Text = "Dolna granica przedziału liczności nominałów.";
            psLblEtykietaDolnejGranicyPrzedziału.Font =
                new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular);
            // ustalenie położenia (lokalizacji) kontrolki na formularzu
            psLblEtykietaDolnejGranicyPrzedziału.Location = new Point(400, 180);
            psLblEtykietaDolnejGranicyPrzedziału.Height = 60;
            psLblEtykietaDolnejGranicyPrzedziału.Width = 150;
            psLblEtykietaDolnejGranicyPrzedziału.BackColor = psTcZakładki.TabPages[1].BackColor;
            psLblEtykietaDolnejGranicyPrzedziału.ForeColor = psTcZakładki.TabPages[1].ForeColor;
            psLblEtykietaDolnejGranicyPrzedziału.Visible = true;
            psTcZakładki.TabPages[1].Controls.Add(psLblEtykietaDolnejGranicyPrzedziału);
            // sformatowanie kontrolki TextBox
            psTxtDolnaGranicaPrzedziału.BackColor = Color.White;
            psTxtDolnaGranicaPrzedziału.ForeColor = Color.Black;
            psTxtDolnaGranicaPrzedziału.Text = "";
            psTxtDolnaGranicaPrzedziału.Font =
                new Font(FontFamily.GenericSansSerif, 12.25F, FontStyle.Bold);
            psTxtDolnaGranicaPrzedziału.TextAlign = HorizontalAlignment.Center;
            // lokalizacja kotrolki
            psTxtDolnaGranicaPrzedziału.Location = new Point(400, 190);
            // dodanie kontrolki do kolekcji Controls zakładki Bankomat
            psTcZakładki.TabPages[1].Controls.Add(psTxtDolnaGranicaPrzedziału);
            psTxtGórnaGranicaPrzedziału.Text = "Górna granica przedziału liczności nominałów.";
            psTxtGórnaGranicaPrzedziału.Font =
                new Font(FontFamily.GenericSansSerif, 10F, FontStyle.Regular);
            psTxtGórnaGranicaPrzedziału.Location = new Point(510, 180);
            psTxtGórnaGranicaPrzedziału.Height = 60;
            psTxtGórnaGranicaPrzedziału.Width = 150;
            psTcZakładki.TabPages[1].Controls.Add(psTxtGórnaGranicaPrzedziału);
            // sformatowanie kontrolki TextBox dla górnej granicy
            psTxtGórnaGranicaPrzedziału.BackColor = Color.White;
            psTxtGórnaGranicaPrzedziału.ForeColor = Color.Black;
            psTxtGórnaGranicaPrzedziału.Font =
                new Font(FontFamily.GenericSansSerif, 12.25F, FontStyle.Bold);
            psTxtDolnaGranicaPrzedziału.Text = "";
            psTxtDolnaGranicaPrzedziału.TextAlign = HorizontalAlignment.Center;
            psTxtGórnaGranicaPrzedziału.Location = new Point(665, 190);
        }
        private void PsBtnAkceptacjaLiczności_Click(object sender, EventArgs e)
        {
            // odsłaniamy kontrolki z wypłatą
            psLblWypłacaneNominały.Visible = true;
            psDgvZawartośćBankomatu.Visible = true;
            // potwierdzenie wypłaty wymaganej kwoty
            psTxtKwotaDoWypłaty.Text = psTxtKwotaDoWypłaty.Text;
            // odłonięcie kontrolek
            psTxtKwotaDoWypłaty.Visible = true;
            psLblWypłacanaKwota.Visible = true;
            psLblKwotaDoWypłaty.Visible = true;
            psBtnAkceptacja.Visible = true;
            psTxtWypłacanaKwota.Visible = true;
            psBtnReset.Visible = true;
            psBtnKoniec.Visible = true;
            psBtnAkceptacjaLiczności.Enabled = false;
            const int psBanknotONajniższejWartości = 10;
            // sprawdzenie, czy została wybrana waluta
            if (psCmbRodzajWaluty.SelectedIndex < 0)
            {
                psErrorProvider1.SetError(psCmbRodzajWaluty, "ERROR: musisz wybrać walutę.");
                return;
            }
            /* sprawdzenie, czy została wybrana kontrolka RadioButton
             *  dla określnmeia sposonu wyznaczania liczności
             */
            if (!(psRdbUstawienieDomyślne.Checked || psRdbUstawienieUżytkownika.Checked))
            {
                psErrorProvider1.SetError(psCmbRodzajWaluty, "ERROR: musisz wybrać sposób na ustalenie liczności nominałów");
                return;
            }
            // ustalenie stanu braku aktywności
            psRdbUstawienieDomyślne.Enabled = false;
            psRdbUstawienieUżytkownika.Enabled = false;
            // rozpoznanie, która kontrolka RadioButton została wybrana
            if (psRdbUstawienieDomyślne.Checked)
            {
                this.psDgvZawartośćBankomatu.EditMode = DataGridViewEditMode.EditProgrammatically;
                // utworzenie egzemplarza generatora liczb losowych
                Random psRnd = new Random();
                for (ushort psi = 0; psi < PsPojemnikNominałów.Length; psi++)
                {
                    PsPojemnikNominałów[psi].PsWartość = psWartośćNominałów[psi];
                    PsPojemnikNominałów[psi].PsLiczność = (ushort)psRnd.Next(psMaxLicznośćNominałów);
                }
                psLblWypłacaneNominały.Text = "Wylosowane liczności nominałów.";

            }
            if (psRdbUstawienieUżytkownika.Checked)
            {
                this.psDgvZawartośćBankomatu.EditMode = DataGridViewEditMode.EditOnEnter;

            }
            // odsłonięcie kontrolek dla wizualizaji ustalonej liczbości nominałów
            psLblWypłacaneNominały.Visible = true;
            psDgvZawartośćBankomatu.Visible = true;
            psDgvZawartośćBankomatu.Rows.Clear();
            // wpisanie liczności nominałów do kontrolki dgv
            for (ushort psi = 0; psi < PsPojemnikNominałów.Length; psi++)
            {
                psDgvZawartośćBankomatu.Rows.Add();
                for (ushort psj = 0; psj < psDgvZawartośćBankomatu.Columns.Count; psj++)
                    psDgvZawartośćBankomatu.Rows[psi].Cells[psj].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (psRdbUstawienieDomyślne.Checked)
                    psDgvZawartośćBankomatu.Rows[psi].Cells[0].Value = PsPojemnikNominałów[psi].PsLiczność;
                if (psRdbUstawienieUżytkownika.Checked)
                {
                    psDgvZawartośćBankomatu.Rows[psi].Cells[0].Value = 10;
                    psLblNotatkaOUstawieniachUżytkownika.Visible = true;
                }
                psDgvZawartośćBankomatu.Rows[psi].Cells[1].Value = PsPojemnikNominałów[psi].PsWartość;
                // wypisanie rodzaju wypłaconego nominału
                if (PsPojemnikNominałów[psi].PsWartość >= psBanknotONajniższejWartości)
                    psDgvZawartośćBankomatu.Rows[psi].Cells[2].Value = "banknot";
                else
                    psDgvZawartośćBankomatu.Rows[psi].Cells[2].Value = "moneta";
                // wypisanie waluty 
                psDgvZawartośćBankomatu.Rows[psi].Cells[3].Value = psCmbRodzajWaluty.SelectedItem;
                // wycentrowanie zapisu w poszczególnych komórkach
                
            }
  

        }
        private void PsPbxPłatnośćZbliżeniowa_Click(object sender, EventArgs e)
        {
            // prosta funkcjonalność płatności kartą
            if (psWybranoProdukt)
                psLblKomunikat.Text = "Płatność przebiegła z powodzeniem.";
            else
                psLblKomunikat.Text = "Proszę wybrać produkt przed płatnością.";
        }
        public void PsCbxWybórWaluty_SelectedIndexChanged(object sender, EventArgs e)
        {
            float psCurrencyMultiplier; // przeliczenie między walutami (mnożnik rozróżniający od złotego)
            psLblKosztWybranegoProduktu.Text = "0"; // wyzerowanie wybranego produktu w związku ze zmianą waluty
            psZapłaconaKwota = 0; PsUpdateSaldo(); // zresetowanie kwoty zapłaconej przez użytkownika
            psDgvListaProduktów.Rows.Clear(); psDgvListaProduktów.Rows.Add(); // wyzerowanie listy zakupów
            psIndexWybranychProduktów = 1; psCenaFloat = 0; PsUpdateSaldo();  // wyzerowanie ceny wybranych produktów
            float[] psDefaultPrice = { 5.00F, 3.30F, 2.80F, 4.10F, 15.0F, 7.5F, 10.3F, 6.8F }; // domyślna psCena produktów w złotówkach
            // PŁATNOŚĆ UŻYTKOWNIKA: listy monet podzielone na waluty
            PictureBox[] psUniwersalneBilony = new PictureBox[] { psPbx20,psPbx10,psPbx5,psPbx2,psPbx1,psPbxPół,
            psPbxDwieDziesiąte,psPbxJednaDziesiąta };
            PictureBox[] psDolary = new PictureBox[] { psPbx20, psPbx10,psPbx5,psPbx2,psPbx1,psPbxPół,psPbxJednaCzwarta,
                psPbxJednaDziesiąta };
            PictureBox[] psJeny = new PictureBox[] { psPbxJeny2000,psPbxJeny1000,psPbxJeny500,psPbxJeny100,psPbxJeny50,
            psPbxJeny10,psPbxJeny5,psPbxJeny1 };
            PictureBox[] psWony = new PictureBox[] { psPbxWon10000,psPbxWon5000,psPbxWon1000,psPbxWon500,psPbxWon100,
            psPbxWon50,psPbxWon10,psPbxWon5 };
            // RESZTY: listy monet podzielone na waluty [używane w wizualizacji reszty] 
            PictureBox[] psResztaUniwersalna = new PictureBox[] { psPbxResztaUniwersalna0,psPbxResztaUniwersalna1,psPbxResztaUniwersalna2,
            psPbxResztaUniwersalna3,psPbxResztaUniwersalna4,psPbxResztaUniwersalna5,psPbxResztaUniwersalna6,psPbxResztaUniwersalna7};
            PictureBox[] psResztaDolary = new PictureBox[] { psPbxResztaUniwersalna0,psPbxResztaUniwersalna1,psPbxResztaUniwersalna2,
            psPbxResztaUniwersalna3,psPsPbxResztaĆwierć,psPbxResztaUniwersalna5,psPbxResztaUniwersalna6,psPbxResztaUniwersalna7};
            PictureBox[] psResztaWony = new PictureBox[] { psPbxResztaWon10000,psPbxResztaWon5000,psPbxResztaWon1000,psPbxResztaWon500,
            psPbxResztaWon100,psPbxResztaWon50,psPbxResztaWon10,psPbxResztaWon5 };
            PictureBox[] psResztaJeny = new PictureBox[] { psPbxResztaJeny0, psPbxResztaJeny1, psPbxResztaJeny2,
            psPbxResztaJeny3,psPbxResztaJeny4,psPbxResztaJeny5,psPbxResztaJeny6,psPbxResztaJeny7 };

            switch (psCbxWybórWaluty.SelectedIndex) // przeliczenie uzależnione od złotego = waluty domyślnej
            {
                case 0:
                default: // PLN
                    psTxt = "zł"; psCurrencyMultiplier = 1;
                    for (int psi = 0; psi < psUniwersalneBilony.Length; psi++)
                    {
                        // ustawianie widoczności wizualizacji monet w sekcji płatności psi reszty
                        psDolary[psi].Visible = false;
                        psUniwersalneBilony[psi].Visible = true;
                        psResztaDolary[psi].Visible = false;
                        psResztaWony[psi].Visible = false;
                        psResztaJeny[psi].Visible = false;
                        psResztaUniwersalna[psi].Visible = true;
                        psJeny[psi].Visible = false;
                        psWony[psi].Visible = false;
                    }
                    break;
                case 1: // USD
                    psTxt = "$"; psCurrencyMultiplier = 0.23F;
                    for (int psi = 0; psi < psUniwersalneBilony.Length; psi++)
                    {
                        // ustawianie widoczności wizualizacji monet w sekcji płatności psi reszty
                        psUniwersalneBilony[psi].Visible = false;
                        psJeny[psi].Visible = false;
                        psDolary[psi].Visible = true;
                        psWony[psi].Visible = false;
                        psResztaUniwersalna[psi].Visible = false;
                        psResztaDolary[psi].Visible = true;
                        psResztaWony[psi].Visible = false;
                        psResztaJeny[psi].Visible = false;
                    }
                    break;
                case 2: // EUR
                    psTxt = "€"; psCurrencyMultiplier = 0.21F;
                    for (int psi = 0; psi < psUniwersalneBilony.Length; psi++)
                    {
                        // ustawianie widoczności wizualizacji monet w sekcji płatności psi reszty
                        psDolary[psi].Visible = false;
                        psUniwersalneBilony[psi].Visible = true;
                        psResztaDolary[psi].Visible = false;
                        psResztaWony[psi].Visible = false;
                        psResztaJeny[psi].Visible = false;
                        psResztaUniwersalna[psi].Visible = true;
                        psJeny[psi].Visible = false;
                        psWony[psi].Visible = false;
                    }
                    break;
                case 3: // GBP
                    psTxt = "£"; psCurrencyMultiplier = 0.18F;
                    for (int psi = 0; psi < psUniwersalneBilony.Length; psi++)
                    {
                        // ustawianie widoczności wizualizacji monet w sekcji płatności psi reszty
                        psDolary[psi].Visible = false;
                        psUniwersalneBilony[psi].Visible = true;
                        psResztaDolary[psi].Visible = false;
                        psResztaWony[psi].Visible = false;
                        psResztaJeny[psi].Visible = false;
                        psResztaUniwersalna[psi].Visible = true;
                        psJeny[psi].Visible = false;
                        psWony[psi].Visible = false;
                    }
                    break;
                case 4:  //JPY
                    psTxt = "¥"; psCurrencyMultiplier = 27.49F;
                    for (int psi = 0; psi < psUniwersalneBilony.Length; psi++)
                    {
                        // ustawianie widoczności wizualizacji monet w sekcji płatności psi reszty
                        psUniwersalneBilony[psi].Visible = false;
                        psJeny[psi].Visible = true;
                        psDolary[psi].Visible = false;
                        psWony[psi].Visible = false;
                        psResztaUniwersalna[psi].Visible = false;
                        psResztaDolary[psi].Visible = false;
                        psResztaWony[psi].Visible = false;
                        psResztaJeny[psi].Visible = true;
                    }
                    break;
                case 5:  // KRW
                    psTxt = "₩"; psCurrencyMultiplier = 288.42F;
                    for (int psi = 0; psi < psUniwersalneBilony.Length; psi++)
                    {
                        // ustawianie widoczności wizualizacji monet w sekcji płatności psi reszty
                        psUniwersalneBilony[psi].Visible = false;
                        psJeny[psi].Visible = false;
                        psDolary[psi].Visible = false;
                        psWony[psi].Visible = true;
                        psResztaUniwersalna[psi].Visible = false;
                        psResztaDolary[psi].Visible = false;
                        psResztaWony[psi].Visible = true;
                        psResztaJeny[psi].Visible = false;
                    }
                    break;
            }
            Label[] psLabels = new Label[]{ psLblDasani, psLblAquafina, psLblSparkletts, psLblEvian, psLblOreo, psLblPepperidge,
               psLblChipsAhoy, psLblNilla }; // lista etykiet produktów do iteracji
            for (int psi = 0; psi < psLabels.Length; psi++) // przekształcenie nazw etykiet w odpowiednie liczby
            {
                if (psWony[psi].Visible) // zaokrąglenie do realistycznej wartości w wonach
                {
                    psDefaultPrice[psi] = ((int)(psDefaultPrice[psi] * psCurrencyMultiplier / 10)) * 10;
                }
                else
                {
                    psDefaultPrice[psi] = psJeny[psi].Visible
                        ? (float)Math.Round(psDefaultPrice[psi] * psCurrencyMultiplier, 0)
                        : (float)Math.Round(psDefaultPrice[psi] * psCurrencyMultiplier, 2);
                }
                psLabels[psi].Text = psDefaultPrice[psi] + psTxt;   // zapisanie cen profuktów do części tekstowej widocznej w aplikacji
            }

        }
        private void PsPictureBox_Click(object sender, EventArgs e)
        {
            psClickedPicture = sender as PictureBox;    // odbiór wciśniętego obrazka od użytkownika
            if (psClickedPicture == null) // dla pewności
                return;
            psWybranoProdukt = true;    // płatność może być realizowana
            PsWykonajPłatność(); psLblReszta.Text = "0";    // wykonanie płatności psi zresetowanie reszty, żeby uniknąć chaosu w następnych zakupach
            if (psRbnNie.Checked)
                psIndexWybranychProduktów++;    // zwiększenie indeksu na wypadek niedecyzyjności użytkownika psi zmiany ustawienia                           // w trakcie działania programu
            else if (psRbnTak.Checked)
                psIndexWybranychProduktów++;    // zwiększenie indeksu produktu w celu zapisu na liście zakupów
            else
            {
                psRbnNie.Checked = true;
                psIndexWybranychProduktów++;    // na wypadek nieprzeiwdzianego błędu
            }

        }
        private void PsUpdateSaldo()
        {
            // wypisanie zapłaconej sumy
            psLblWłożoneMonety.Text = string.Format("{0:0.00}", psZapłaconaKwota.ToString() + psTxt);
            psReszta = psZapłaconaKwota - psCenaFloat;
            // jeżeli użytkownik chce kupić tylko jedną rzecz, jest automatycznie przekierowany do zakończenia zakupów
            if ((psZapłaconaKwota > psCenaFloat) && (psRbnNie.Checked))
                PsPrzeprowadźWydawanieReszty();
        }
        private void PsWykonajPłatność()
        {
            string psName; // pojemnik na nazwę kupowanego produktu
            // pozyskiwanie nazw produktów z nazw etykiet produktów
            string[] psLabelNames = new string[] { psLblDasani.Name,psLblAquafina.Name,psLblSparkletts.Name,psLblEvian.Name,
            psLblOreo.Name,psLblPepperidge.Name,psLblChipsAhoy.Name,psLblNilla.Name };
            Label[] psLabels = new Label[]{ psLblDasani, psLblAquafina, psLblSparkletts, psLblEvian, psLblOreo, psLblPepperidge,
               psLblChipsAhoy, psLblNilla }; // lista etykiet produktów do iteracji
            if (psWybranoProdukt)
            {
                // z nazwy wybranej ikony produktu pobierana jest nazwa produktu
                psName = psClickedPicture.Name.ToString().Substring(psClickedPicture.Name.ToString().IndexOf("x") + 1);
                // nazwy etykiet wszystkich produktów są pobrane
                for (int psi = 0; psi < psLabelNames.Length; psi++)
                {
                    psLabelNames[psi] = psLabelNames[psi].ToString().Substring(psLabelNames[psi].ToString().IndexOf("l") + 1);
                }
                psLblKomunikat.Text = ($"Wybrano produkt: '{psName}'.");
                if (psRbnTak.Checked)
                    psLblKomunikat.Text = ($"Wybrano produkt: '{psName}'. Żeby wykonać płatność wciśnij przycisk CHECKOUT.");
                // w liście etykiet szukana jest pobrana nazwa wybranego produktu/obrazu, z listy cen wpisanych w tekst etykiet
                // znajdowana jest pasująca cena w formacie string
                psCena = psLabels[Array.IndexOf(psLabelNames, psName)].Text;
                // cena przystosowana jest do konwersji w formę liczbową
                if (psTxt == "zł")
                    psCena = psCena.TrimEnd().Substring(0, psCena.Length - 2);
                else
                    psCena = psCena.TrimEnd().Substring(0, psCena.Length - 1);
                if (psRbnNie.Checked)
                {
                    psCenaFloat = float.Parse(psCena, CultureInfo.InvariantCulture.NumberFormat);
                    psIndexWybranychProduktów = 1;
                }
                else if (psRbnTak.Checked)
                {
                    psCenaFloat += float.Parse(psCena, CultureInfo.InvariantCulture.NumberFormat);
                }
                /* jeśli użytkownik wybrał opcję jednego produktu, index pozostaje na poziomie 1, a produkt wybrany przez
                 użytkownika jest nadpisywany z każdym kliknięciem. Jeśli wybrana jest opcja wielu produktów, index wzrasta
                 z każdym wyborem, a każdy kolejny produkt zapisywany jest w następnym rzędzie. */
                if (psIndexWybranychProduktów > 1)
                    psDgvListaProduktów.Rows.Add();
                psLblKosztWybranegoProduktu.Text = psCenaFloat.ToString() + psTxt;
                // zapisanie produktów do tabeli / na listę zakupów
                psDgvListaProduktów.Rows[psIndexWybranychProduktów - 1].Cells[0].Value = psName;
                psDgvListaProduktów.Rows[psIndexWybranychProduktów - 1].Cells[1].Value = psCena + psTxt;
            }
        }
        private void PsPrzeprowadźWydawanieReszty()
        {
            // zapobieganie niesforności liczb floatowych
            psReszta = (float)Math.Round(psReszta, 2);
            psLblReszta.Text = string.Format("{0:0.00}", psReszta.ToString() + psTxt);
            if ((psPbxWon10000.Visible == true) || (psPbxJeny2000.Visible == true))
                psLblKosztWybranegoProduktu.Text = "0" + psTxt;
            else
                psLblKosztWybranegoProduktu.Text = "0.00" + psTxt;
            psLblKomunikat.Text = "Dziękujemy za zakupy . . . ";
            // wyzerowanie wszystkich wskaźników:
            PsWydanieReszty();
            psCena = "0"; psCenaFloat = 0;
            psDgvListaProduktów.Rows.Clear();
            psDgvListaProduktów.Rows.Add();
            psZapłaconaKwota = 0; psIndexWybranychProduktów = 1;
        }
        private void PsBtnCheckout_Click(object sender, EventArgs e)
        {
            // przygotowanie zmiennej do ogłoszenia użytkownikowi, ile pieniędzy zabrakło do wykonania płatności
            float psZapłać = psCenaFloat - psZapłaconaKwota;
            psZapłać = (float)Math.Round(psZapłać, 2);

            if (psZapłaconaKwota > psCenaFloat)
            {
                PsPrzeprowadźWydawanieReszty();
                psLblKomunikat.ForeColor = Color.FromArgb(240, 240, 240);
            }
            else // jeśli monety wybrane przez użytkownika nie są wystarczające, żeby pokryć zakupy
            {
                psLblKomunikat.ForeColor = Color.Red;
                psLblKomunikat.Text = "Brakujące środki: " + psZapłać + psTxt;
            }
            Debug.WriteLine($"{psCenaFloat}, {psZapłaconaKwota}, {psSumaLosowania}");
        }
        private void PsBtnAnuluj_Click(object sender, EventArgs e)
        {
            // przerwanie tranzakcji psi wykonanie metody, która wyzeruje wszystkie wskaźniki
            psLblKomunikat.ForeColor = Color.FromArgb(240, 240, 240);
            psLblKomunikat.Text = "Zakupy zostały anulowane. Pięniądze zostaną zwrócone.";
            PsAnulujTranzakcję();
        }
        private void PsAnulujTranzakcję()
        {
            // metoda, która wyzeruje wszystkie wskaźniki
            psZapłaconaKwota = 0; psCenaFloat = 0; PsUpdateSaldo();
            psDgvListaProduktów.Rows.Clear();
            psDgvListaProduktów.Rows.Add();
            psDgvReszta.Rows.Clear();
            psLblKosztWybranegoProduktu.Text = "0"; psIndexWybranychProduktów = 1;
        }
        private void PsWydanieReszty()
        {

            //const int psLiczbaMonet = 50; int[] psWylosowaneNominały = new int[psLiczbaMonet]; int[] psPodliczenie = new int[8];
            int[] psNaliczenie = new int[8]; // licznik zużytych przez program monet do reszty
            float[,] waluty = new float[,] {  { 5, 2, 1, 0.5F, 0.2F, 0.1F, 0.05F, 0.01F },   // PLN, EUR, GBP
                                              { 5, 2, 1, 0.5F, 0.25F, 0.1F, 0.05F, 0.01F }, // USD
                                              { 2000, 1000, 500, 100, 50, 10, 5, 1 },       // JPY
                                              { 10000, 5000, 1000, 500, 100, 50, 10, 5 } }; // KRW
            // podliczenie, ile monet każdego rodzaju wylosował automat będzie szło do zmiennej psLiczbaNominału
            // licznik, ile monet danego rodzaju zostało wykorzystanych w wydawaniu reszty (nie może przekroczyć psLiczbaNominału) to psPodliczenie
            int psIndex = 0; // index monety wybranej do reszty
            float psSuma = 0; // suma wartości pobranych monet

            Debug.WriteLine(psReszta);
            psDgvReszta.Visible = true; // odsłonięcie tabeli psi etykiety z listą monet składających się na resztę
            psLblLListaWydMonet.Visible = true;
            // wyzerowanie tabeli przed ponownym użyciem
            psDgvReszta.Rows.Clear();
            int psWybórWaluty;
            // przygotowanie do switcha
            if (psPbxResztaUniwersalna0.Visible)
                psWybórWaluty = 0;
            else if (psPsPbxResztaĆwierć.Visible)
                psWybórWaluty = 1;
            else if (psPbxResztaJeny0.Visible)
                psWybórWaluty = 2;
            else if (psPbxResztaWon10000.Visible)
                psWybórWaluty = 3;
            else
                psWybórWaluty = 0;

            while (psReszta > 0)
            {
                for (int psh = 0; psh < 4; psh++)
                {
                    if (psWybórWaluty == psh)
                        for (int psi = 0; psi < waluty.GetLength(1); psi++)
                        {
                            psSumaLosowania = psPodliczenie[psi] * waluty[psh, psi];
                            if (psSumaLosowania < psReszta)
                            {
                                psLblKomunikat.Text = "Nie ma wystarczających środków, by wypłacić resztę. Przepraszamy.";
                                return;
                            }
                            for (int psj = 0; psj < 100; psj++)
                            {
                                if (psReszta == 0) // w momencie, gdy reszta dosięga zera, funkcja jest przerwana
                                    break;
                                if (waluty[psh, psi] > psReszta)
                                    psi++; // dogonienie poziomu reszty przez wybrany poziom monety (wystarczająco niski nominał)
                                else if (psPodliczenie[psi] > psNaliczenie[psi]) // automat może zwrócić wyłącznie pieniądze, które posiada
                                    if (waluty[psh, psi] <= psReszta)
                                    { // kiedy nominał monety jest wystarczająco niski, jest brany pod uwagę do reszty
                                        psNaliczenie[psi]++; // licznik wybrania monety danego rodzaju wzrasta
                                        Debug.WriteLine($"Naliczenie: {psNaliczenie[psi]}, Wylosowanego: {psPodliczenie[psi]}");
                                        psReszta -= waluty[psh, psi]; // obliczenie nowej wartości reszty
                                        psReszta = (float)Math.Round(psReszta, 2);
                                        psSuma = (float)Math.Round(psSuma, 2);
                                        psSuma += waluty[psh, psi]; // obliczenie obecnej sumy zwracanej reszty
                                        psDgvReszta.Rows.Add(); // zapisanie wyników do tabeli z resztą
                                        psDgvReszta.Rows[psIndex].Cells[0].Value = psIndex + 1;
                                        psDgvReszta.Rows[psIndex].Cells[1].Value = waluty[psh, psi];
                                        psDgvReszta.Rows[psIndex].Cells[2].Value = psSuma;
                                        Debug.WriteLine(psj + " - " + psIndex + ", psSuma monet: " + psSuma + ", psReszta: " + psReszta);
                                        psj++; psIndex++;
                                    }
                                    else
                                        psi++;
                            }
                        }
                }
            }
            Debug.WriteLine(psSumaLosowania);
            // jako że kwota znajdująca się w bankomacie jest losowa, może zdażyć się, że automat nie będzie miał jak zwrócić reszty
            if (psSumaLosowania < psReszta)
            {
                psLblKomunikat.Text = "W automacie nie ma wystarczającej ilości pięniędzy. Tranzakcja anulowana.";
                PsAnulujTranzakcję();
                return;
            }
        }
        public void PsLosoweNominały()
        {
            Random psRnd = new Random();

            Label[] psEtykietyLosowania = new Label[]{ psLblIlość0, psLblIlość1, psLblIlość2, psLblIlość3, psLblIlość4,
            psLblIlość5, psLblIlość6, psLblIlość7 }; // lista etykiet produktów do iteracji
            for (int psi = 0; psi < psWylosowaneNominały.Length; psi++)
            {
                psWylosowaneNominały[psi] = psRnd.Next(0, 8);
                for (int psj = 0; psj < psPodliczenie.Length; psj++)
                    if (psj == psWylosowaneNominały[psi])
                        psPodliczenie[psj]++;
            }
            for (int psi = 0; psi < psPodliczenie.Length; psi++)
                psEtykietyLosowania[psi].Text = "x " + psPodliczenie[psi];

            Console.WriteLine("[{0}]", string.Join(", ", psWylosowaneNominały));
            Console.WriteLine("[{0}]", string.Join(", ", psPodliczenie));
        }
        public void PsPięniądzeDostępneWAutomacie()
        {
            Random psRnd = new Random();
            int psLiczbaNominałów = psRnd.Next(40, 60);
            int[] psWylosowaneNominały = new int[psLiczbaNominałów];
            float[] psWartośćNominałówWAutomacieUniwersalne = { 5, 2, 1, 0.5F, 0.2F, 0.1F, 0.05F, 0.01F }; // 8 el.
            Label[] psUniversalLabels = new Label[]{ psLblIlość0, psLblIlość1, psLblIlość2, psLblIlość3, psLblIlość4,
            psLblIlość5, psLblIlość6, psLblIlość7 }; // lista etykiet produktów do iteracji
            for (int psi = 0; psi < psWylosowaneNominały.Length; psi++)
            {
                psWylosowaneNominały[psi] = psRnd.Next(0, psWartośćNominałówWAutomacieUniwersalne.Length);
            }
            // Set the size of psCount to maximum value in numbers + 1
            int[] psCount = new int[psWartośćNominałówWAutomacieUniwersalne.Length];
            for (int psi = 0; psi < psWylosowaneNominały.Length - 1; psi++)
            {
                if (psWylosowaneNominały[psi] > 0 && psWylosowaneNominały[psi] < psWylosowaneNominały.Length)
                {
                    // Use value from numbers as the psIndex for psCount and increment the psCount
                    psCount[psWylosowaneNominały[psi]]++;
                }
            }
            foreach (int psi in psCount)
            {
                // Check all values in psCount
                Console.WriteLine("{0}: {1}", psWartośćNominałówWAutomacieUniwersalne[psi], psCount[psi - 1]);
                psUniversalLabels[psi].Text = "x " + psCount[psi];
                psSumaLosowania += psWartośćNominałówWAutomacieUniwersalne[psi] * psCount[psi];
            }
            Debug.WriteLine(psSumaLosowania);
        }
        private void PsZapłata(object sender, EventArgs e)
        {
            string psNazwaMonety; // moneta wybrana przez użytkownika
            if (psWybranoProdukt)
            {
                psWybórMonety = sender as PictureBox;
                if (psWybórMonety == null) // just to be on the safe side
                    psNazwaMonety = "";
                if (psPbx20.Visible)
                {
                    if (psWybórMonety == psPbxPół)
                        psNazwaMonety = "0.5";
                    else if (psWybórMonety == psPbxDwieDziesiąte)
                        psNazwaMonety = "0.2";
                    else if (psWybórMonety == psPbxJednaDziesiąta)
                        psNazwaMonety = "0.1";
                    else if (psWybórMonety == psPbxJednaCzwarta)
                        psNazwaMonety = "0.25";
                    else
                        psNazwaMonety = psWybórMonety.Name.ToString().Substring(psWybórMonety.Name.ToString().IndexOf("x") + 1);
                }
                else if (psPbxWon10000.Visible)
                    psNazwaMonety = psWybórMonety.Name.ToString().Substring(psWybórMonety.Name.ToString().IndexOf("n") + 1);
                else if (psPbxJeny2000.Visible)
                    psNazwaMonety = psWybórMonety.Name.ToString().Substring(psWybórMonety.Name.ToString().IndexOf("y") + 1);
                else
                    psNazwaMonety = null;
                psZapłaconaKwota += float.Parse(psNazwaMonety, CultureInfo.InvariantCulture.NumberFormat);
                Debug.WriteLine(psZapłaconaKwota);
                PsUpdateSaldo();
            }
            else
                psLblKomunikat.Text = "Wybierz produkt zanim zaczniesz płacić.";
        }
        private void PsBtnUsuńProdukt_Click(object sender, EventArgs e)
        {
            int psSelectedIndex = psDgvListaProduktów.CurrentCell.RowIndex;
            if (psSelectedIndex > -1)
            {
                string psMsg = String.Format("{0}", psDgvListaProduktów.Rows[psSelectedIndex].Cells[1].Value);
                if (psTxt == "zł")
                    psMsg = psMsg.TrimEnd().Substring(0, psMsg.Length - 2);
                else
                    psMsg = psMsg.TrimEnd().Substring(0, psMsg.Length - 1);
                Debug.WriteLine($"{psMsg}, {psCenaFloat}");
                psCenaFloat -= float.Parse(psMsg, CultureInfo.InvariantCulture.NumberFormat);
                psCenaFloat = (float)Math.Round(psCenaFloat, 2);
                psLblKosztWybranegoProduktu.Text = psCenaFloat.ToString() + psTxt;
                Debug.WriteLine($"{psMsg}, {psCenaFloat}");
                psDgvListaProduktów.Rows.RemoveAt(psSelectedIndex);
                psDgvListaProduktów.Refresh();
                psIndexWybranychProduktów--;
            }
        }

        private void btnAkceptacja_Click(object sender, EventArgs e)
        {

            // ustawienie stanu braku aktywności dla przycisku Akceptacja
            psBtnAkceptacja.Enabled = false;
            float psKwotaDoWypłaty = float.Parse(psTxtKwotaDoWypłaty.Text, CultureInfo.InvariantCulture.NumberFormat);
            if (!PsCzyWypłataMożeByćZrealizowana(PsPojemnikNominałów, psKwotaDoWypłaty))
            {
                // w bankomacie nie ma odpowiedniego kapitału (liczby nominałów)
                psErrorProvider1.SetError(psBtnAkceptacjaLiczności, "ERROR: nie możemy zrealizować" +
                    "tak dużej wpłaty. Spróbuj pobrać mniejszą kwotę.");
            }
            // realizacja wypłaty
            // deklaracja zmiennej płatniczej
            psResztaDoWypłaty = psKwotaDoWypłaty;
            // rozpoczynami wypłatę od największych nominałów, czyli od pierwszej pozycji PojemnikaNominałów
            ushort psIndexPojemnikaNominałów = 0;
            // odsłonięcie atrybutów kontrolek dla prezentacji wypłaty
            // zmiana tytułu kontrolki Label opisującej kontrolkę DataGridView
            psLblWypłacaneNominały.Text = "Wypłacane nominały: ";
            // iteracyjne dokonywanie wypłaty
            ushort psLiczbaNominałów, psIndexDGV = 0; psDgvZawartośćBankomatu.Rows.Clear();
            while ((psResztaDoWypłaty > 0.0F) && (psIndexPojemnikaNominałów < PsPojemnikNominałów.Length))
            {
                // policzenie, ile nominałów byłoby potrzebnych dla zrealizowania wypłaty
                psLiczbaNominałów = (ushort)(psResztaDoWypłaty / PsPojemnikNominałów[psIndexPojemnikaNominałów].PsWartość);
                // sprawdzenie, czy na pozycji psIndexPojemnikaNominałów w pojemniku nominałów
                if (psLiczbaNominałów > PsPojemnikNominałów[psIndexPojemnikaNominałów].PsLiczność)
                {
                    // w pozycji psIndexPojemnikaNominałów nie ma wymaganej liczności nominałów
                    //więc pobieramy wszystkie nominały
                    psLiczbaNominałów = PsPojemnikNominałów[psIndexPojemnikaNominałów].PsLiczność;
                    // wyzerowanie liczności noinałów na pozycji psIndexPojemnikaNominałów
                    PsPojemnikNominałów[psIndexPojemnikaNominałów].PsLiczność = 0;
                }
                else
                {
                    /* z pozycji psIndexPojemnikaNominałów pobieramy nominały o liczności czyli psLiczbaNominałów */
                    PsPojemnikNominałów[psIndexPojemnikaNominałów].PsLiczność = (ushort)(PsPojemnikNominałów[psIndexPojemnikaNominałów].PsLiczność - psLiczbaNominałów);
                }
                // sokonanie wpłaty nominałów o liczności psLiczbaNominałów
                if (psLiczbaNominałów > 0)
                {
                    // dodanie nowego / pustego wiersza
                    psDgvZawartośćBankomatu.Rows.Add();
                    // wypełnienie poszególnych pól / komórek dodanego wiersz do kontrolki DGV
                    psDgvZawartośćBankomatu.Rows[psIndexDGV].Cells[0].Value = psLiczbaNominałów;
                    psDgvZawartośćBankomatu.Rows[psIndexDGV].Cells[1].Value = PsPojemnikNominałów[psIndexPojemnikaNominałów].PsWartość;
                    // wypisanie rodzaju wypłaconego nominału
                    if (PsPojemnikNominałów[psIndexDGV].PsWartość >= 10) // psBanknotONajniższejWartości
                        psDgvZawartośćBankomatu.Rows[psIndexDGV].Cells[2].Value = "banknot";
                    else
                        psDgvZawartośćBankomatu.Rows[psIndexDGV].Cells[2].Value = "moneta";
                    // wypisanie waluty 
                    psDgvZawartośćBankomatu.Rows[psIndexDGV].Cells[3].Value = psCmbRodzajWaluty.SelectedItem;
                    // wycentrowanie zapisu w poszczególnych komórkach
                    psIndexDGV++;
                }
                // uaktualnienie reszty do wpłaty
                psResztaDoWypłaty -= psLiczbaNominałów * PsPojemnikNominałów[psIndexPojemnikaNominałów].PsWartość;
                // przejście do następnej pozycji pojemnika nominałów
                psIndexPojemnikaNominałów++;
            } while ((psResztaDoWypłaty > 0) && (psIndexPojemnikaNominałów < PsPojemnikNominałów.Length)) ;
            // sprawdzenie, czy wszystko zostało wypłacone
            if (psResztaDoWypłaty > 0)
            {
                // nie wypłaciliśmy pełnej kwoty
                psErrorProvider1.SetError(psBtnAkceptacjaLiczności, "PRZEPRASZAMY, ale nie możemy wypłacić pełnej kwoty," +
                    "gdyż brakuje odpowiednich nominałów.");
                // ukrycie kontrolek z wypłatą nominałów
                psLblWypłacaneNominały.Visible = false;
                psDgvZawartośćBankomatu.Visible = false;
            }
            else
            {
                
            }
            psTxtWypłacanaKwota.Text = psTxtKwotaDoWypłaty.Text;
        }

        private void btnKoniec_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            // ustawienie początkowe kontrolki wyboru waluty
            psCmbRodzajWaluty.SelectedIndex = -1;
            psCmbRodzajWaluty.Text = "Lista walut (wybierz jedną z nich)";
            psCmbRodzajWaluty.Enabled = true;
            // ustawienie początkowe kontrolek wypłaty
            psTxtKwotaDoWypłaty.Text = "";
            psTxtKwotaDoWypłaty.Enabled = false;
            psLblWypłacanaKwota.Visible = false;
            psTxtWypłacanaKwota.Visible = false;
            // ukrycie przycisków operacyjnych
            psBtnReset.Visible = false;
            psBtnKoniec.Visible = false;
            // przywrócenie stanu początkowego kontrolek do określenia liczności nominałów
            psBtnAkceptacjaLiczności.Enabled = true;
            psBtnAkceptacja.Enabled = true;
            psTxtKwotaDoWypłaty.Enabled=true;
            // ustawienie braku zaznaczenia kontrolek Radiobutton
            psRdbUstawienieDomyślne.Checked = false;
            psRdbUstawienieUżytkownika.Checked = false;
            // ustawienie stanu aktywności kontrolek Radiobutton
            psRdbUstawienieDomyślne.Enabled = true;
            psRdbUstawienieUżytkownika.Enabled = true;
        }

        private void txtKwotaDoWypłaty_KeyPress(object sender, KeyPressEventArgs e)
        {
            /* metoda mająca na celu zezwolenie na wpisanie wyłącznie cyfr odpowiednich dla floata, z zezwoleniem na jedną kropkę
                w celu stworzenia liczby dziesiętnej */
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            // zezwolenie na jedną kropkę
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }

        }
    }
 }

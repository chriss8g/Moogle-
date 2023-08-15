namespace MoogleEngine;

public class Metodos{
    static char[] delimitadores={' ',',',';','.','(',')','?', '"', '\''};

    

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////Metodos del Constructor////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //este metodo crea un array con las propiedades de los documentos(fichas)😁
    static public ficha[] Destripador(string[] ficheros){

        int CantDoc= ficheros.Length;
        ficha[] DocumentFolder=new ficha[CantDoc];

        for (int i = 0; i < CantDoc; i++){
            DocumentFolder[i]=new ficha(ficheros[i]);       //instancia de la clase ficha
        }
        return DocumentFolder;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //el proximo metodo recibe el array de fichas para agnadirle el TFIDF de cada palabra y dejar el IDF separado tambien para la query luego😁
    static public void CreaTFIDF(ref ficha[] DocumentFolder,ref Dictionary<string, float> IDF){
        int CantDoc=DocumentFolder.Length;

        for(int i=0;i<CantDoc;i++){
            DocumentFolder[i].Descriptor=new Dictionary<string, List<int>>();          //inicializando el array de dictionaries
            DocumentFolder[i].TFIDF=new Dictionary<string, float>();
        }
        

        for (int j=0; j<CantDoc; j++){          //por cada documento
            float FrecuenciaUno=1.0f/DocumentFolder[j].Peso; 
            int conta=0;                        //este contador es para quedarme con la posicion de cada palabra en el texto y facilitarme luego el trabajo para el operador ~
            foreach(string word in DocumentFolder[j].WordsArray){ //por cada palabra del i-esimo documeto

                if(!DocumentFolder[j].Descriptor.ContainsKey(word)){
                    DocumentFolder[j].Descriptor[word]=new List<int>();   //se detecta una palabra nueva
                    DocumentFolder[j].TFIDF[word]=0.0f;
                }
                DocumentFolder[j].TFIDF[word]+=FrecuenciaUno;  //aumenta en una unidad por cada ocurrencia de la palabra en el documento, note que al final del proceso se tendra el TF
                DocumentFolder[j].Descriptor[word].Add(conta);

                if(!IDF.ContainsKey(word)){
                    IDF[word]=0.0f;
                }
                conta++;
            }

            foreach (string word in DocumentFolder[j].TFIDF.Keys){     //para el IDF
                IDF[word]++;
            }
        }


        foreach (string i in IDF.Keys){
            IDF[i]= (float)Math.Log((CantDoc +1.0f)/IDF[i]);
        }//IDF listos!

        for (int i = 0; i < CantDoc; i++){
            foreach (string item in DocumentFolder[i].TFIDF.Keys){   
                DocumentFolder[i].TFIDF[item]*=IDF[item];
            }
        }//TFIDF listos!
    }


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////Metodos de Query////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //TF-IDF de la consulta😁
    static public Dictionary<string, float> PalTFIDFCons(string consulta, Dictionary<string, float> IDF,ref Queue <string> important2,ref ficha[]DocumentFolder){
        
        string [] safador=consulta.Split( delimitadores, System.StringSplitOptions.RemoveEmptyEntries );

        int TamagnoConsulta=safador.Length;

        //aqui guardaremos los datos a devolver: palabra(q aprezca en los documentos) asociada a su TFIDF
        Dictionary<string, float> ConsTFIDF = new Dictionary<string, float>();

        //esto asosiara cada posible palabra con su tf-idf(contendra toooodas las posibles palabras en los documentos)
        Dictionary<string, float> tfcon = new Dictionary<string, float>();
 
        foreach (string item in safador){       //inicializando cada tf-idf en cero
            tfcon[item] = 0.0f;
            ConsTFIDF[item] = 0.0f;
        }

        OperayTF(ref tfcon, ref safador, ref DocumentFolder);
        Comillas (consulta, ref DocumentFolder);
        ComillasSimples (consulta, ref DocumentFolder);

        foreach (string item in safador){       //TFIDF!!
            if ( ! IDF.ContainsKey(item)){
                continue;
            }
            ConsTFIDF[item] = tfcon[item] * IDF[item];
        }

        //esto es para ordenar las palabras de la consulta, en una cola acorde a su importancia para el tema del snippet luego
        string ipo = ""; //ipo sera mi palabra mejor ranqueada
        for (int i = 0; i < safador.Length; i ++){ //hay q inicializar ipo en una palabra cualquieta de la consulta q exista en mis archivos
            if(IDF.ContainsKey(safador[i])){
                ipo = safador[i];
                break;
            }
        }
        while(important2.Count != safador.Length && ipo != ""){        
            for (int i = 0; i < safador.Length; i++){        
                if(IDF.ContainsKey(safador[i]) && (IDF[safador[i]] * tfcon[safador[i]] > IDF[ipo] * tfcon[ipo]) ){
                    ipo = safador[i];
                }
            }
            important2.Enqueue(ipo);
            tfcon[ipo] = 0;
        }


        return ConsTFIDF;
    }


    static void OperayTF(ref Dictionary<string, float> tfcon, ref string []safador, ref ficha[] DocumentFolder){

        int TamagnoConsulta = safador.Length;

        for(int i = 0; i<safador.Length; i++){                //sacando los TFs 
            
            string word = safador[i];
            int multiplicador = 0;

            switch (safador[i][0])
            {
                case '!':

                    safador[i] = safador[i].Substring(1);
                    for (int j = 0; j < DocumentFolder.Length; j++){
                        if(DocumentFolder[j].TFIDF.ContainsKey(safador[i])){   //si aparece ! delante de alguna palabra, todo documento que tenga dicha palabra tendra score=0
                            DocumentFolder[j].Score = 0;
                        }
                    }
                break;

                case '^':
                    safador[i] = safador[i].Substring(1);
                    for (int j = 0; j < DocumentFolder.Length; j++){
                        if(!DocumentFolder[j].TFIDF.ContainsKey(safador[i])){  ////si aparece ^ delante de alguna palabra, todo documento que no tenga dicha palabra tendra score=0
                            DocumentFolder[j].Score = 0;
                        }
                    }
                break;

                case '*':
                    do{
                        multiplicador ++;
                        safador[i] = safador[i].Substring(1);
                    }while(safador[i][0] == '*');                //para que el efecto de * sea aditivo

                    for (int j = 0; j < DocumentFolder.Length; j++){//se multiplica el score por la cantidad de asteriscos, por cada palabra; entre la cantidad de palabras en el documento
                        if(DocumentFolder[j].TFIDF.ContainsKey(safador[i])){
                            DocumentFolder[j].Score *= 2*multiplicador;
                        }
                    }
                break;
            }
            
            if (safador[i] == "~"){
                if(i == 0 || i == safador.Length)
                    break;
                else
                    Cercania(safador[i - 1], safador[i + 1], ref DocumentFolder);
                continue;
            }


            //bloque para revisar que la palabra esta en los documentos
            int cont=0;
            for (int j = 0; j < DocumentFolder.Length; j++){
                if (!DocumentFolder[j].TFIDF.ContainsKey(safador[i])){
                    cont++;
                }
            }
            if (cont == DocumentFolder.Length){
                continue;
            }

            if(!tfcon.ContainsKey(safador[i])){ //Por si la palabra tenia operadores limpiarselos. La condicional es importante
                tfcon[safador[i]] = tfcon[word];
                tfcon.Remove(word);
            }

            //TF de la consulta
            tfcon[safador[i]] += (1.0f / TamagnoConsulta);
        }
    }


    static void Cercania(string uno, string dos, ref ficha[] DocumentFolder){
        for (int i = 0; i < DocumentFolder.Length; i++){
            
            float cercano = int.MaxValue;
            if (DocumentFolder[i].Descriptor.ContainsKey(uno) && DocumentFolder[i].Descriptor.ContainsKey(dos)){
                for (int j = 0; j < DocumentFolder[i].Descriptor[uno].Count; j++){
                    for (int k = 0; k < DocumentFolder[i].Descriptor[dos].Count; k++){
                        cercano= Math.Min(Math.Abs(DocumentFolder[i].Descriptor[uno][j] - DocumentFolder[i].Descriptor[dos][k]), cercano);
                    }
                }
                DocumentFolder[i].Score *= 9 * ((float)Math.Pow(((cercano / DocumentFolder[i].Peso) - 1.0f), 2)) + 1; //esta formula es para multiplicar mi score por un numero entre 1 y 10.....mientras mas cercanas esten las palabras mejor
            }
        }
    }


    static void Comillas(string consulta, ref ficha[] DocumentFolder){
        string literal = "";    //para almacenar el texto entre ""
        bool abierto = false;   //indica si las comillas estan abiertas
        foreach (char item in consulta){

            if (item == '"'){      //si las comillas estaban abiertas cierralas y el documento que no contenga esa frase anulalo
                if (abierto){
                    abierto = false;
                    for (int j = 0; j < DocumentFolder.Length; j++){
                        if(DocumentFolder[j].Contenido.IndexOf(literal) == -1){  
                            DocumentFolder[j].Score = 0;
                        }
                    }
                    continue;
                }
            }

            if (abierto){   //si las comillas estan abiertas aumenta el literal
                literal += item;
            }

            if (item == '"'){      //si las comillas estaban cerradas, abrelas
                if(!abierto)
                    abierto = true;
            }
        }
    }

    //el siguiente metodo es la contrapartida de Comillas(), su implementacion es analoga
    static void ComillasSimples(string consulta, ref ficha[] DocumentFolder){
        string literal = "";    //para almacenar el texto entre ""
        bool abierto = false;   //indica si las comillas estan abiertas
        foreach (char item in consulta){

            if (item == '\''){      //si las comillas estaban abiertas cierralas y el documento que contenga esa frase anulalo
                if (abierto){
                    abierto = false;
                    for (int j = 0; j < DocumentFolder.Length; j++){
                        if(DocumentFolder[j].Contenido.IndexOf(literal) != -1){ 
                            DocumentFolder[j].Score = 0;
                        }
                    }
                    continue;
                }
            }

            if (abierto){   //si las comillas estan abiertas aumenta el literal
                literal += item;
            }

            if (item == '\''){      //si las comillas estaban cerradas abrelas
                if(!abierto)
                    abierto = true;
            }
        }
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
    //aqui hallamos el coseno del angulo entre los dos vectores😁
    static public void HallaCoseno(Dictionary<string, float> ConsTFIDF, ref ficha[] DocumentFolder){

        int cantidad = DocumentFolder.Length;
        int PesoQuery = ConsTFIDF.Keys.Count;
        float[,] BD = new float[cantidad + 1, PesoQuery];
        //en esta matriz a continuacion se plasmaran los valores de TFIDF de los documentos y la query, para una mejor "vision vectorial"
        for (int i = 0; i <= cantidad; i++){
            int j = 0;
            foreach (string item in ConsTFIDF.Keys){
                if(i != cantidad){
                    BD[i,j] = DocumentFolder[i].TFIDF.ContainsKey(item) ? DocumentFolder[i].TFIDF[item] : 0;
                }
                else{
                    BD[i,j] = ConsTFIDF[item];
                }
                j++;
            }
        }


        for(int i = 0; i < cantidad; i++){

            float escalar = 0;   //producto escalar
            float MoDoc = 0; //modulo del vector documento i
            float Moquery = 0;   //modulo del vector query

            foreach (string item in DocumentFolder[i].TFIDF.Keys){
                MoDoc += (float)Math.Pow(DocumentFolder[i].TFIDF[item],2);
            }

            for (int j = 0; j < PesoQuery; j++){
                escalar += BD[i,j] * BD[cantidad,j];              //en estas lineas y una d abajo, es que se halla el coseno!
                Moquery += BD[cantidad,j] * BD[cantidad,j];  
            }

            if(Moquery == 0){     
                DocumentFolder[i].Score = 0;
                continue;
            }
            else{
                DocumentFolder[i].Score *= (float)(escalar/Math.Sqrt(MoDoc*Moquery));       //coseno aqui
            }
        }
    }    

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //Snippet!!😁
    static public void BuscaSnippet(ref ficha[] DocumentFolder, int[] orden, string[] important, int canti ){ 
        
        for (int i = 0; i < canti; i++){   

            int inicio = 0;
            int CantCaract = DocumentFolder[orden[i]].Contenido.Length;
            for (int j = 0; j < important.Length; j++){
                int cont = 0;
                bool brea = false;        //explicacion debajo
                int inicio2;
                inicio2 = DocumentFolder[orden[i]].Contenido.IndexOf(important[j], cont);      
                    //la posicion en el i-esimo archivo del resultado
                    //de la j-esima palbra mas importante

                if(inicio2 == -1)
                    continue;

                do{
                    inicio = inicio2;

                    //esto es para q no usara una palabra como parte de otra en el snippet
                    char t = '.';
                    char q = '.';
                    
                    try{        //este try se usa para que si la palabra encontrada es la ultima del documento no de overflow
                        t = DocumentFolder[orden[i]].Contenido[inicio+important[j].Length];
                        q = DocumentFolder[orden[i]].Contenido[inicio - 1];
                    }
                    catch (System.Exception){   
                        t = ' ';
                    }
                    
                    
                    
                    brea=false;
                    foreach (char item in delimitadores){
                        if(t == item){
                            foreach (char items in delimitadores){
                                if(q == items){
                                    brea = true;
                                }
                            }
                        }              
                    }

                }while( ( ( cont = inicio + 1) != CantCaract )    &&    ( ( inicio2 = DocumentFolder[orden[i]].Contenido.IndexOf(important[j], cont)) != -1) && !brea);

                if(brea){
                    break;
                }
  
            }

            if (inicio == -1 || important.Length == 0){   //si no aparecio ninguna palabra o no no hay palabras conincidentes entre los documentos y la query inicia en el final
                inicio = CantCaract;
            }


            int rango = CantCaract - inicio;

            int CantidadCaracteresSnippet = 300;      //el nombre habla por si solo
            if (rango > CantidadCaracteresSnippet)      
                rango = CantidadCaracteresSnippet;

            DocumentFolder[orden[i]].Snippet = "";

            for (int j = 0; j < rango; j++){

                DocumentFolder[orden[i]].Snippet += DocumentFolder[orden[i]].Contenido[inicio+j];
            }
            
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////

    static public string HallaFactorSugerencia(string consulta, Dictionary<string, float> IDF){
        string [] safador = consulta.Split( delimitadores, System.StringSplitOptions.RemoveEmptyEntries );
        int PalabQuery = safador.Length;
        string Sugerencia = "";
        
        string wordmin = " ";

        for (int i = 0; i < PalabQuery; i++)        //compara cada palabra en la query
        {
            int minimo = int.MaxValue;
            foreach (string word in IDF.Keys)       //con las palabras en mi base de datos
            {
                int p = DosPalabras(safador[i], word);        //aplica el levanchtein ese
                if (p < minimo){          //escoge mis mejores opciones
                    wordmin = word;
                    minimo = p;
                }
            }
            Sugerencia += wordmin+" ";
        }
        return Sugerencia;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////

    static int DosPalabras(string a, string b){     //Levenshtein
        
        int[,] matri = new int[a.Length+1,b.Length+1];
        int coste = 0;


        for (int i = 0; i <= a.Length; i++) 
        {
            matri[i,0] = i;
        }
        for (int j = 0; j <= b.Length; j++)
        {
            matri[0,j] = j;
        }


        for (int i = 1; i <= a.Length; i++){
            for (int j = 1; j <= b.Length; j++){
                if(a[i-1] == b[j-1]){
                    coste = 0;
                }
                else{
                    coste = 1;
                }
                int deletion = matri[i-1,j]+1;
                int insertion = matri[i,j-1]+1;
                int substitution = matri[i-1,j-1]+coste;
                matri[i,j] = Math.Min(Math.Min(deletion,insertion), Math.Min(insertion,substitution));
                
            }
        }
        return matri[a.Length, b.Length];
    }

}

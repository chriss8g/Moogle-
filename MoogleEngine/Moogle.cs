namespace MoogleEngine;


public class Moogle{

    static string [] ficheros;                              //para guardar la ruta de los archivos
    static ficha[] DocumentFolder;                          //para guardar las propiedades de los documentos
    static Dictionary<string, float> IDF=new Dictionary <string, float>();  //asocia cada palabra con su IDF
    static Dictionary<string, float> ConsTFIDF;             //asocia cada palabra de la query con su TFIDF
    static SearchItem [] items;                             //array con los resultados a devolver
    static int Cantitems;                                   //cantidad maxima de resultados a devolver
    static Queue<string> important2= new Queue<string>();   //palabras de la query ordenadas por importancia  
   


    public Moogle(){    //Constructor!!!

        ficheros=Directory.GetFiles("../Content", "*.txt");
        //rutas de los archivos


        DocumentFolder=Metodos.Destripador(ficheros);
        //array con las propiedades de los documentos(fichas)
        
    
        Metodos.CreaTFIDF(ref DocumentFolder,ref IDF);
        //TF-IDF de las palabras: en DocumentFolder[i].Descriptor[word][0] se guarda el TFIDF de la palabra word en el i-esimo documeto

        Cantitems =7;
        //para imprimir las coincidencias
    }

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public SearchResult Query(string consulta) {

        //limpia si hubo una busqueda anterior
        for (int i = 0; i < DocumentFolder.Length; i++){
            DocumentFolder[i].Score = 1.0f;  //comienza en uno pq los incrementos los hago con el producto usual
        }
        important2.Clear();            //lista que contendra las palabras d la query en orden d importancia para el snnipet
        string sugerencia = "";       
        

        //TF-IDF de la consulta
        ConsTFIDF = Metodos.PalTFIDFCons(consulta, IDF,ref important2, ref DocumentFolder);

        Metodos.HallaCoseno(ConsTFIDF,ref DocumentFolder);
        //asocia indice del elemento en ficheros con su similitud respecto a la consulta


        float []Coseno=new float[DocumentFolder.Length];    //guardaremos una copia d los cosenos aqui para poder sobrescribirlos sin perderlos
        for (int i = 0; i < DocumentFolder.Length; i++){
            Coseno[i]=DocumentFolder[i].Score;
        }
        //crea un array 'orden', con el orden de los indices en ficheros 
        //que deben devolverse segun la consulta
        int[] orden = new int[DocumentFolder.Length];
        for (int j = 0; j < orden.Length; j++){
            orden[j] = j;
            for(int i=0; i < Coseno.Length; i++){
                if(Coseno[i] > Coseno[orden[j]]){
                    orden[j] = i;
                }
            }
            Coseno[orden[j]] = -1;
        }


        int conteo = 0;
        for (int i = 0; i < Cantitems; i++){
            if(DocumentFolder[orden[i]].Score > 0.0f){
                conteo++;                           //cuantos de los resultados en items son realmente coincidencias?, o sea, no me des documentos con score 0
            }
        }

        string []important =important2.ToArray();           //convierte la cola, de palabras importantes en us array
        Metodos.BuscaSnippet(ref DocumentFolder, orden, important, conteo); //se trabaja con snippet por referencia para trabajar dentro del metodo con el tamagno que ya le dimos a este y asi ahorrarnos otro parametro
        
        items=new SearchItem[conteo];
        for(int i=0;i<conteo;i++) {

            items[i]=new SearchItem(DocumentFolder[orden[i]].Name, DocumentFolder[orden[i]].Snippet, DocumentFolder[orden[i]].Score);
        }

        if(conteo == 0)
            sugerencia=Metodos.HallaFactorSugerencia(consulta, IDF);


        return new SearchResult(items, sugerencia);
    }
}


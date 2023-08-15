namespace MoogleEngine;

public class ficha{
    static char[] delimitadores={' ',',',';','.','(',')','?', '"', '\''};

    private string ruta;        //URL del documento
    public string Ruta{
        get{return ruta;}
    }

    private string name;       //nombre del documento
    public string Name{
        get{return name;}
    }

    private string contenido;   //texto en el documetno
    public string Contenido{
        get{return contenido;}
    }

    private string [] wordsArray;   //array con el texto del documetno separado en palabras
    public string[] WordsArray{
        get{return wordsArray;}
    }

    private int peso;       //cantidad de palabras en el documento
    public int Peso{
        get{return peso;}
    }

    private float score;    //similitud del documetno respecto a una query dada
    public float Score{
        get{return score;}
        set{score=value;}
    }

    private string snippet; //fragmento del documento coincidente con una query dada
    public string Snippet{
        get{return snippet;}
        set{snippet=value;}
    }

    private Dictionary<string, float> tfidf;
    public Dictionary<string, float> TFIDF{
            get{return tfidf;}
            set{tfidf=value;}
    }

    private Dictionary<string, List<int>> descriptor; //asocia cada palabra con una lista que contiene: en su primera posicion
    public Dictionary<string, List<int>> Descriptor{ //el TFIDF de la palabra, y luego la posicion de todas las ocurrencias de
        get{return descriptor;}                       //la palabra en el documento.(esto ultimo para facilitar el operador ~)
        set{descriptor=value;}
    }

    public ficha(string root){
        ruta=root;
        name=ruta.Split('/')[ruta.Split('/').Length-1];
        name=name.Replace(".txt","");
        contenido=File.ReadAllText(ruta); //lee el texto en el documento dado
        wordsArray=contenido.Split( delimitadores, System.StringSplitOptions.RemoveEmptyEntries ); //divide el texto en palabras
        //for (int i = 0; i < wordsArray.Length; i++){wordsArray[i]=wordsArray[i].ToLower();}
        peso=wordsArray.Length;
        snippet="";
    }

}
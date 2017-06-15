<Query Kind="FSharpProgram">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

type MyKey = { Id : string}

type StringConverter() =
    inherit JsonConverter()
    override this.CanWrite with get() = true
    override this.WriteJson (writer,value,serializer) = 
            let t = Newtonsoft.Json.Linq.JToken.FromObject(value)
            t.WriteTo(writer)
    override this.ReadJson (reader,objectType,existingVakue,serializer) = 
        let (t:Newtonsoft.Json.Linq.JValue) = Newtonsoft.Json.Linq.JValue.Load(reader) :?> Newtonsoft.Json.Linq.JValue
        t.Value
    override this.CanConvert objectType = objectType = typedefof<string>



type DictionaryJsonConverter() =
    inherit JsonConverter()
    
    let addObjectToDictionary (reader:JsonReader) (result:Map<_,_>) (serializer:JsonSerializer) keyType valueType =
        let mutable (key:MyKey Option) = None
        let mutable value = null
        let mutable finished = false
        let mutable myResult = result

        while reader.Read() && (not finished) do
            if reader.TokenType = JsonToken.EndObject && key <> None then
                myResult <- result.Add(key.Value, value)
                finished <- true
            else
                let propertyName = reader.Value.ToString()
            
                if propertyName = "Key" then
                    do reader.Read() |> ignore
                    key <- Some (serializer.Deserialize<_>(reader)) //, keyType
                elif propertyName = "Value" then
                    do reader.Read() |> ignore
                    value <- serializer.Deserialize(reader, valueType) //, valueType)
                    
        myResult
    
    
    let xx (serializer:JsonSerializer) (writer:JsonWriter) (dictionary:Map<_,_>) key value =                    
        do writer.WriteStartObject()
        do writer.WritePropertyName("Key")
        do serializer.Serialize(writer, key)
        do writer.WritePropertyName("Value")
        do serializer.Serialize(writer, dictionary |> Map.find key)
        do writer.WriteEndObject()
    
    override this.CanWrite with get() = true
    override this.CanRead with get() = false
    
    override this.WriteJson (writer,value,serializer) = 
        let dictionary = value :?> Map<MyKey,string>
        let f = xx serializer writer dictionary
        writer.WriteStartArray()
        do dictionary |> Map.iter f
        writer.WriteEndArray()

    override this.ReadJson (reader,objectType,existingVakue,serializer) = 
        
        if not (this.CanConvert(objectType)) then
            raise (Exception (sprintf "This converter is not for %A." objectType))

        let keyType = objectType.GetGenericArguments().[0]
        let valueType = objectType.GetGenericArguments().[1]
        let dictionaryType = typedefof<Map<_,_>>.MakeGenericType(keyType, valueType)
        let mutable (result:Map<_,_>) = Map.empty

        if reader.TokenType = JsonToken.Null then
            null        
        else
            let mutable finished = false
            while reader.Read() && (not finished) do
                if reader.TokenType = JsonToken.EndArray then
                    finished <- true
                elif reader.TokenType = JsonToken.StartObject then
                    result <- addObjectToDictionary reader result serializer keyType valueType
            result :> Object
        

    override this.CanConvert t = 
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Map<_, _>>
   


let k = { Id = "a" }
let m = Map.empty |> Map.add k "a"

let serialized = JsonConvert.SerializeObject(m, new DictionaryJsonConverter())
let s = JsonConvert.DeserializeObject<Map<MyKey, string>>(serialized, new DictionaryJsonConverter());
s |> Map.find k |> Dump
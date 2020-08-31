using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EDA_PROJECT_1314;

namespace MyContactsStore
{
    public class MyContactsStore : IContactsStore
    {
        public int T { get; set; }
        public BTree btName { get; set; }
        public BTree btPhone { get; set; }
        public BTree btAge { get; set; }
        public Manager Manag { get; set; }

        public MyContactsStore()
        {
            T=10;
        }

        public void AddContact(IPerson person)
        {
            btName=btName.Insert(person, Manag);
            btPhone=btPhone.Insert(person, Manag);
            btAge=btAge.Insert(person, Manag);
        }

        public void Close()
        {
            Manag.Close(btName, btPhone, btAge);
        }

        public IEnumerable<IPerson> FindByAge(int age)
        {
            foreach (var item in btAge.FindByAge(age,btAge,Manag))
                yield return item;                
        }

        public IEnumerable<IPerson> FindByName(string preffixName)
        {
            foreach (var item in btName.FindPrefixName(preffixName,btName,Manag))
                yield return item;                
        }

        public IEnumerable<IPerson> FindByPhone(string preffixPhone)
        {
            foreach (var item in btPhone.FindPrefixPhone(preffixPhone,btPhone,Manag))
                yield return item;                
        }

        public IEnumerable<IPerson> FindOlder(int age)
        {
            foreach (var item in btAge.FindOlder(age,btAge,Manag))
                yield return item;
        }

        public IEnumerable<IPerson> FindYounger(int age)
        {
            foreach (var item in btAge.FindYouger(age, btAge, Manag))
                yield return item;
        }

        public void Open(System.IO.Stream db)
        {
            Manag = new Manager(db,T);
            Manag.EnableCache(10);
            btName = Manag.Intitialize(Manag.RootN, new CompareByName());
            btPhone = Manag.Intitialize(Manag.RootP, new CompareByPhone());
            btAge = Manag.Intitialize(Manag.RootA, new CompareByAge());
        }

        public string AuthorFullName
        {
            get { return "Marco A. Cardoso Gutierrez"; }
        }

        public Group AuthorGroup
        {
            get { return Group.C211; }
        }


        #region MY CODE

        public static bool find;
        public static bool end;

        public class BTree
        {
            #region PROPERTIES

            public long Position { get; set; }

            public int Size { get; private set; }

            public bool Leaf { get; set; }

            public long[] Children { get; set; }

            public bool Root { get; set; }

            public KeysStore KeySt { get; set; }

            public IComparer<IPerson> Comparer { get; set; }

            #endregion

            #region CONSTRUCTORS

            public BTree(int Size, IComparer<IPerson> Comparer) //BUILD ROOT
            {
                this.Position = Position;
                this.Root = true;
                this.Size = (Size * 2) - 1;
                KeySt = new KeysStore(this.Size, Comparer);
                this.Comparer = Comparer;
                //Children = new BTree[this.Size + 1];
                Children = new long[this.Size + 1];
                Leaf = true;
                //ID = 0;
            }

            public BTree(BTree Parent) //BUILD NODE
            {
                this.Position = Position;
                this.Root = false; ;
                this.Size = Parent.Size;
                KeySt = new KeysStore(this.Size, Parent.Comparer);
                //Children = new BTree[this.Size + 1];
                Children = new long[this.Size + 1];
                //ID = 0;
                this.Comparer = Parent.Comparer;
                Leaf = true;
            }


            #endregion

            #region METHODS

            public BTree Insert(IPerson insertion, Manager manag)
            {
                if (Root && KeySt.Full) // => FULL ROOT
                {
                    BTree nRoot = new BTree((Size + 1) / 2, Comparer);
                    manag.UpdatePos();
                    nRoot.Position = manag.LastInsertion; // REVISAR!!!

                    this.Root = false;
                    nRoot.ChildInsertion(this.Position, 0);
                    SplitChild(this, nRoot, 0, manag);
                    InsertNonFull(nRoot, insertion, manag);
                    return nRoot;
                }

                else
                    InsertNonFull(this, insertion, manag);
                return this;
            }

            public void InsertNonFull(BTree bT, IPerson insertion, Manager manag)
            {
                BTree temp = bT;
                while (true)
                {
                    if (temp.Leaf)
                    {
                        temp.KeySt.Insert(insertion);
                        break;
                    }

                    else
                    {
                        int index = temp.KeySt.Search(insertion);

                        if (index < 0)
                            index = index * (-1) - 1;

                        BTree tempChild = manag.Load(temp.Children[index], Comparer);

                        if (tempChild == null)
                            tempChild = new BTree(temp);

                        if (tempChild.KeySt.Full)
                            SplitChild(tempChild, temp, index, manag); //REVISAR i o i+1
                        else
                            temp = tempChild;
                    }
                }
                manag.Save(temp);
            }

            public void SplitChild(BTree btY, BTree btX, int index, Manager manag)
            {
                BTree btZ = manag.Allocate(Comparer);
                btZ.Leaf = btY.Leaf;
                btZ.Position = manag.LastInsertion;
                int middle = btY.Size / 2;
                int c = btY.KeySt.LastInsertion - (middle + 1);
                int j = 0;
                long tBT;
                
                while (c > 0)
                {
                    if (!btY.Leaf)
                    {
                        tBT = btY.ChildExtraction(middle + 1);
                        btZ.ChildInsertion(tBT, j);
                    }

                    IPerson temp = btY.KeySt.Extract(middle + 1);
                    btZ.KeySt.Insert(temp);
                    c--;
                    j++;
                }
                if (!btY.Leaf)
                {
                    tBT = btY.ChildExtraction(middle + 1);
                    btZ.ChildInsertion(tBT, j);
                }

                IPerson t = btY.KeySt.Extract(middle);
                btX.KeySt.InsertAt(t, index);

                btX.ChildInsertion(btZ.Position, index + 1);
                manag.Save(btX);
                manag.Save(btY);
                manag.Save(btZ);
            }

            public void ChildInsertion(long nChild, int index)
            {
                Leaf = false;
                long temp1 = Children[index];
                Children[index] = nChild;
                
                for (int i = index + 1; i < KeySt.LastInsertion + 1; i++)
                {
                    long temp2 = Children[i];

                    Children[i] = temp1;

                    temp1 = temp2;
                }
            }

            public long ChildExtraction(int index)
            {
                long res = Children[index];

                long temp1 = Children[KeySt.LastInsertion];
                Children[KeySt.LastInsertion] = 0; //QQQQQQ

                for (int i = KeySt.LastInsertion - 1; i >= index; i--)
                {
                    long temp2 = Children[i];

                    Children[i] = temp1;

                    temp1 = temp2;
                }
                return res;
            }

            #region FIND

            #region BY NAME
            public IEnumerable<IPerson> FindPrefixName(string prefix, BTree actual, Manager manag)
            {
                end = false;
                find = false;
                foreach (var item in PFindPrefixName(prefix, actual, manag))
                    yield return item;
            }

            private IEnumerable<IPerson> PFindPrefixName(string prefix, BTree actual, Manager manag)
            {
                if (actual.Leaf)
                {
                    for (int i = 0; i < actual.KeySt.LastInsertion; i++)
                    {
                        if (IsPrefix(prefix, actual.KeySt.Keys[i].Name))
                        {
                            yield return actual.KeySt.Keys[i];
                            find = true;
                        }
                        else if (find)
                        {
                            end = true;
                            yield break;
                        }
                    }
                }

                else
                {
                    for (int i = 0; i < actual.KeySt.LastInsertion; i++)
                    {
                        if (prefix.CompareTo(actual.KeySt.Keys[i].Name) <= 0)
                        {
                            foreach (var item in PFindPrefixName(prefix, manag.Load(actual.Children[i], actual.Comparer), manag))
                                yield return item;
                            if (end)
                                yield break;
                            if (IsPrefix(prefix, actual.KeySt.Keys[i].Name))
                            {
                                find = true;
                                yield return actual.KeySt.Keys[i];
                            }
                            else if (find)
                            {
                                end = true;
                                yield break;
                            }
                        }
                    }
                    foreach (var item in PFindPrefixName(prefix, manag.Load(actual.Children[actual.KeySt.LastInsertion], actual.Comparer), manag))
                    {
                        yield return item;
                    }
                }
                yield break;
            }
            #endregion

            #region BY PHONE
            public IEnumerable<IPerson> FindPrefixPhone(string prefix, BTree actual, Manager manag)
            {
                end = false;
                find = false;
                foreach (var item in PFindPrefixPhone(prefix, actual, manag))
                    yield return item;
            }

            private IEnumerable<IPerson> PFindPrefixPhone(string prefix, BTree actual, Manager manag)
            {
                if (actual.Leaf)
                {
                    for (int i = 0; i < actual.KeySt.LastInsertion; i++)
                    {
                        if (IsPrefix(prefix, actual.KeySt.Keys[i].Phone))
                        {
                            yield return actual.KeySt.Keys[i];
                            find = true;
                        }
                        else if (find)
                        {
                            end = true;
                            yield break;
                        }
                    }
                }

                else
                {
                    for (int i = 0; i < actual.KeySt.LastInsertion; i++)
                    {
                        if (prefix.CompareTo(actual.KeySt.Keys[i].Phone) <= 0)
                        {
                            foreach (var item in PFindPrefixPhone(prefix, manag.Load(actual.Children[i], actual.Comparer), manag))
                                yield return item;
                            if (end)
                                yield break;
                            if (IsPrefix(prefix, actual.KeySt.Keys[i].Phone))
                            {
                                find = true;
                                yield return actual.KeySt.Keys[i];
                            }
                            else if (find)
                            {
                                end = true;
                                yield break;
                            }
                        }
                    }
                    foreach (var item in PFindPrefixPhone(prefix, manag.Load(actual.Children[actual.KeySt.LastInsertion], actual.Comparer), manag))
                    {
                        yield return item;
                    }
                }

                yield break;
            }
            #endregion

            #region BY AGE

            public IEnumerable<IPerson> FindYouger(int age, BTree actual, Manager manag)
            {
                end = false;
                find = false;
                foreach (var item in PFindYouger(age, actual, manag))
                    yield return item;
            }
            private IEnumerable<IPerson> PFindYouger(int age, BTree actual, Manager manag)
            {
                if (actual.Leaf)
                {
                    for (int i = 0; i < actual.KeySt.LastInsertion; i++)
                    {
                        if (age > actual.KeySt.Keys[i].Age)
                        {
                            yield return actual.KeySt.Keys[i];
                            find = true;
                        }
                        else if (find)
                        {
                            end = true;
                            yield break;
                        }
                    }
                }

                else
                {
                    for (int i = 0; i < actual.KeySt.LastInsertion; i++)
                    {
                            foreach (var item in PFindYouger(age, manag.Load(actual.Children[i], actual.Comparer), manag))
                                yield return item;
                            if (end)
                                yield break;
                            if (age > actual.KeySt.Keys[i].Age)
                            {
                                find = true;
                                yield return actual.KeySt.Keys[i];
                            }
                            else 
                            {
                                end = true;
                                yield break;
                            }
                    }
                    foreach (var item in PFindYouger(age, manag.Load(actual.Children[actual.KeySt.LastInsertion], actual.Comparer), manag))
                    {
                        yield return item;
                    }

                }
            }


            public IEnumerable<IPerson> FindOlder(int age, BTree actual, Manager manag)
            {
                end = false;
                find = false;
                foreach (var item in PFindOlder(age, actual, manag))
                    yield return item;
            }
            private IEnumerable<IPerson> PFindOlder(int age, BTree actual, Manager manag)
            {
                if (actual.Leaf)
                {
                    for (int i = 0; i < actual.KeySt.LastInsertion; i++)
                    {
                        if (age < actual.KeySt.Keys[i].Age)
                        {
                            yield return actual.KeySt.Keys[i];
                            find = true;
                        }
                        else if (find)
                        {
                            end = true;
                            yield break;
                        }
                    }
                }

                else
                {
                    for (int i = 0; i < actual.KeySt.LastInsertion; i++)
                    {
                        if (age < actual.KeySt.Keys[i].Age)
                        {
                            foreach (var item in PFindOlder(age, manag.Load(actual.Children[i], actual.Comparer), manag))
                                yield return item;
                            if (end)
                                yield break;
                            if (age < actual.KeySt.Keys[i].Age)
                            {
                                find = true;
                                yield return actual.KeySt.Keys[i];
                            }
                            else if (find)
                            {
                                end = true;
                                yield break;
                            }
                        }
                    }
                    foreach (var item in PFindOlder(age, manag.Load(actual.Children[actual.KeySt.LastInsertion], actual.Comparer), manag))
                    {
                        yield return item;
                    }

                }
            }


            public IEnumerable<IPerson> FindByAge(int age, BTree actual, Manager manag)
            {
                end = false;
                find = false;
                foreach (var item in PFindByAge(age, actual, manag))
                    yield return item;
            }
            private IEnumerable<IPerson> PFindByAge(int age, BTree actual, Manager manag)
            {
                if (actual.Leaf)
                {
                    for (int i = 0; i < actual.KeySt.LastInsertion; i++)
                    {
                        if (age == actual.KeySt.Keys[i].Age)
                        {
                            yield return actual.KeySt.Keys[i];
                            find = true;
                        }
                        else if (find)
                        {
                            end = true;
                            yield break;
                        }
                    }
                }

                else
                {
                    for (int i = 0; i < actual.KeySt.LastInsertion; i++)
                    {
                        if (age <= actual.KeySt.Keys[i].Age)
                        {
                            foreach (var item in PFindByAge(age, manag.Load(actual.Children[i], actual.Comparer), manag))
                                yield return item;
                            if (end)
                                yield break;
                            if (age == actual.KeySt.Keys[i].Age)
                            {
                                find = true;
                                yield return actual.KeySt.Keys[i];
                            }
                            else if (find)
                            {
                                end = true;
                                yield break;
                            }
                        }
                    }
                    foreach (var item in PFindByAge(age, manag.Load(actual.Children[actual.KeySt.LastInsertion], actual.Comparer), manag))
                    {
                        yield return item;
                    }

                }
            }

            #endregion

            private bool IsPrefix(string prefix, string word)
            {
                if (word.Length < prefix.Length)
                    return false;
                for (int i = 0; i < prefix.Length; i++)
                {
                    if (prefix[i] != word[i])
                        return false;
                }
                return true;
            }
            #endregion

            #endregion
        }

        public class KeysStore
        {
            #region PROPERTIES

            public IPerson[] Keys { get; set; }
            public int LastInsertion { get; private set; }
            public bool Full
            {
                get { return LastInsertion >= Size; }
            }
            public int Size { get; set; }

            public IComparer<IPerson> Comparer { get; set; }

            #endregion

            public KeysStore(int Size, IComparer<IPerson> Comparer)
            {
                this.Size = Size;
                Keys = new IPerson[Size];
                LastInsertion = 0;
                this.Comparer = Comparer;

            }

            #region PUBLIC METHODS

            public void InsertAt(IPerson nKey, int index)
            {
                if (index < 0)
                    index = index * (-1) - 1;


                LastInsertion++;
                InternalInsertion(nKey, index);

            }

            public void Insert(IPerson nKey)
            {
                int index = Array.BinarySearch(Keys, 0, LastInsertion, nKey, Comparer);
                if (index < 0)
                    index = index * (-1) - 1;


                LastInsertion++;
                InternalInsertion(nKey, index);


            }

            public int Search(IPerson nKey)
            {
                return Array.BinarySearch(Keys, 0, LastInsertion, nKey, Comparer);
            }

            public IPerson Extract(int index) //DELETE AND RETURN THE KEY & ID
            {
                LastInsertion--;
                return InternalExtraction(index);
            }

            #endregion

            #region PRIVATE METHODS

            private void InternalInsertion(IPerson nKey, int index)  //INSERTION IN KEYS AND ID ARRAYS
            {
                IPerson temp1 = Keys[index];

                Keys[index] = nKey;

                for (int i = index + 1; i < LastInsertion; i++)
                {
                    IPerson temp2 = Keys[i];

                    Keys[i] = temp1;

                    temp1 = temp2;
                }
            }

            private IPerson InternalExtraction(int index)
            {
                IPerson res = Keys[index];

                IPerson temp1 = Keys[LastInsertion];
                Keys[LastInsertion] = null; //QQQQQQQQQ

                for (int i = LastInsertion - 1; i >= index; i--)
                {
                    IPerson temp2 = Keys[i];

                    Keys[i] = temp1;

                    temp1 = temp2;
                }

                return res;
            }

            #endregion
        }

        public class Person : IPerson
        {
            public int Age { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }

            public Person(int Age, string Name, string Phone)
            {
                this.Age = Age;
                this.Name = Name;
                this.Phone = Phone;
            }

        }

        #region COMPARERS

        class CompareByName : IComparer<IPerson>
        {
            public int Compare(IPerson x, IPerson y)
            {
                int res = x.Name.CompareTo(y.Name);
                if (res == 0)
                    return -1;
                return res;
            }
        }

        class CompareByPhone : IComparer<IPerson>
        {
            public int Compare(IPerson x, IPerson y)
            {
                int res = x.Phone.CompareTo(y.Phone);
                if (res == 0)
                    return -1;
                return res;
            }
        }

        class CompareByAge : IComparer<IPerson>
        {
            public int Compare(IPerson x, IPerson y)
            {
                int res = x.Age.CompareTo(y.Age);
                if (res == 0)
                    return -1;
                return res;
            }
        }

        #endregion

        public class Manager
        {
            BTree[] Cache;
            bool enableCache;

            public int T { get; set; }
            public System.IO.BinaryReader Reader { get; set; }
            public System.IO.BinaryWriter Writer { get; set; }
            public System.IO.Stream Stream { get; set; }
            public int BTreeSize { get; set; }
            public long LastInsertion { get; set; }

            public long RootN { get; set; }
            public long RootA { get; set; }
            public long RootP { get; set; }

            public Manager(System.IO.Stream Stream, int T = 0)
            {
                //System.IO.FileStream fl;
                this.T = T;
                BTreeSize = 224 * T - 98;
                this.Stream = Stream;
                Reader = new System.IO.BinaryReader(Stream);
                Writer = new System.IO.BinaryWriter(Stream);

                if (Stream.Length == 0) // => FILE IS EMPTY
                {
                    Stream.Position = 28;
                    RootN = -1;
                    RootP = -1;
                    RootA = -1;
                    LastInsertion = 28;
                }

                else //=> EXIST DATA SAVE IN THE FILE
                {
                    byte[] buffer = new byte[28];
                    Stream.Position = 0;
                    Reader.Read(buffer, 0, 28);

                    System.IO.MemoryStream memStream = new System.IO.MemoryStream(buffer);
                    System.IO.BinaryReader tempBReader = new System.IO.BinaryReader(memStream);
                    T = tempBReader.ReadInt32();     //DEGREE OF BTREE
                    RootN = tempBReader.ReadInt64(); //POSITION OF THE BTREE ROOTS
                    RootP = tempBReader.ReadInt64();
                    RootA = tempBReader.ReadInt64();
                    LastInsertion = Stream.Length;
                }

            }
            
            public BTree Allocate(IComparer<IPerson> comparer)
            {
                BTree temp = new BTree(T, comparer);
                temp.Root = false;
                UpdatePos();

                return temp;
            }

            public void UpdatePos()
            {
                LastInsertion += BTreeSize;
                Stream.Position = LastInsertion;
            }

            public void Save(BTree bt)
            {
                if (enableCache)
                    UpdateCache(bt);
                else
                    DirectSave(bt);
            }

            private void DirectSave(BTree bt)
            {
                byte[] buffer = new byte[BTreeSize];
                System.IO.MemoryStream memStream = new System.IO.MemoryStream(buffer);
                System.IO.BinaryWriter tempBWriter = new System.IO.BinaryWriter(memStream);

                tempBWriter.Write(bt.Root);
                tempBWriter.Write(bt.Leaf);
                tempBWriter.Write(bt.KeySt.LastInsertion);

                for (int i = 0; i < bt.KeySt.LastInsertion; i++)
                {
                    IPerson tempPerson = bt.KeySt.Keys[i];
                    tempBWriter.Write(tempPerson.Name);
                    tempBWriter.Write(tempPerson.Phone);
                    tempBWriter.Write(tempPerson.Age);
                }

                if (!bt.Leaf)
                {
                    for (int i = 0; i <= bt.KeySt.LastInsertion; i++)
                        tempBWriter.Write(bt.Children[i]);
                }

                Stream.Position = bt.Position;
                Writer.Write(buffer, 0, BTreeSize);
            }

            public BTree Load(long pos, IComparer<IPerson> comparer)
            {
                if (enableCache) 
                {
                    int idx = (int)(pos % Cache.Length);
                    if (Cache[idx] != null && Cache[idx].Position==pos)
                        return Cache[idx];
                }

                Stream.Position = pos;
                byte[] buffer = new byte[BTreeSize];
                Reader.Read(buffer, 0, BTreeSize);
                System.IO.MemoryStream memStream = new System.IO.MemoryStream(buffer);
                System.IO.BinaryReader temBReader = new System.IO.BinaryReader(memStream);

                BTree bt = new BTree(T, comparer);
                bt.Position = pos;

                bt.Root = temBReader.ReadBoolean();
                bt.Leaf = temBReader.ReadBoolean();
                int lastIns = temBReader.ReadInt32();

                for (int i = 0; i < lastIns; i++)
                {
                    string name = temBReader.ReadString();
                    string phone = temBReader.ReadString();
                    int age = temBReader.ReadInt32();

                    bt.KeySt.InsertAt(new Person(age, name, phone), i);
                }

                if (!bt.Leaf)
                {
                    for (int i = 0; i <= lastIns; i++)
                        bt.Children[i] = temBReader.ReadInt64();
                }
                return bt;

            }

            public void Close(BTree btName, BTree btPhone, BTree btAge)
            {
                if (enableCache)
                {
                    for (int i = 0; i < Cache.Length; i++)
                    {
                        if(Cache[i]!=null)
                            DirectSave(Cache[i]);
                    }
                }

                byte[] buffer = new byte[28];
                System.IO.MemoryStream memStream = new System.IO.MemoryStream(buffer);
                System.IO.BinaryWriter tempBWriter = new System.IO.BinaryWriter(memStream);

                tempBWriter.Write(T);
                tempBWriter.Write(btName.Position);
                tempBWriter.Write(btPhone.Position);
                tempBWriter.Write(btAge.Position);

                Stream.Position = 0;
                Writer.Write(buffer, 0, 28);

                Stream.Flush();
                Stream.Close();
                Stream.Dispose();
                Stream = null;
            }

            public BTree Intitialize(long rootPos, IComparer<IPerson> Comparer)
            {
                BTree res;
                if (rootPos == -1)
                {
                    res = new BTree(T, Comparer);
                    res.Position = LastInsertion;
                    UpdatePos();
                }
                else
                    res = Load(rootPos, Comparer);
                return res;
            }

            /// <summary>
            /// The parameter Size means how many MB have the cache
            /// </summary>
            /// <param name="MB"></param>
            public void EnableCache(int MB)
            {
                int convertion = (MB * 1048576) / BTreeSize;
                Cache = new BTree[convertion];
                enableCache = true;
            }
           
            private void UpdateCache(BTree newBT)
            {
                int i = (int)(newBT.Position % Cache.Length);
                if (Cache[i] != null && Cache[i].Position!=newBT.Position)
                    DirectSave(Cache[i]);   
                Cache[i] = newBT;                
            }
        }
        #endregion
    }
}

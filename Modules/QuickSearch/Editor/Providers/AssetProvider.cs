// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Search.Providers
{
    static class AssetProvider
    {
        enum IdentifierType { kNullIdentifier = 0, kImportedAsset = 1, kSceneObject = 2, kSourceAsset = 3, kBuiltInAsset = 4 };

        internal struct AssetMetaInfo
        {
            public readonly string path;
            private readonly string gidString;
            public readonly SearchDocumentFlags flags;

            private GlobalObjectId m_GID;
            public GlobalObjectId gid
            {
                get
                {
                    if (m_GID.assetGUID == default)
                    {
                        if (gidString != null && GlobalObjectId.TryParse(gidString, out m_GID))
                            return m_GID;

                        if (!string.IsNullOrEmpty(path))
                        {
                            m_GID = GetGID(path);
                            return m_GID;
                        }

                        throw new Exception($"Failed to resolve GID for {path}, {gidString}");
                    }

                    return m_GID;
                }
            }

            private string m_Source;
            public string source
            {
                get
                {
                    if (m_Source == null)
                        m_Source = AssetDatabase.GUIDToAssetPath(guid);
                    return m_Source;
                }
            }

            public string guid => gid.assetGUID.ToString();

            private bool m_HasType;
            private Type m_Type;
            public Type type
            {
                get
                {
                    if (!m_HasType)
                    {
                        if (source.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                            m_Type = AssetDatabase.GetTypeFromPathAndFileID(source, (long)gid.targetObjectId);
                        else if (flags.HasAll(SearchDocumentFlags.Nested | SearchDocumentFlags.Asset))
                        {
                            m_Type = AssetDatabase.GetTypeFromVisibleGUIDAndLocalFileIdentifier(gid.assetGUID, (long)gid.targetObjectId);
                            if (m_Type == null)
                                m_Type = obj?.GetType();
                        }

                        if (m_Type == null)
                            m_Type = AssetDatabase.GetMainAssetTypeAtPath(source);
                        m_HasType = true;
                    }
                    return m_Type;
                }
            }

            private Object m_Object;
            public Object obj
            {
                get
                {
                    if (!m_Object)
                    {
                        if (gid.identifierType == (int)IdentifierType.kBuiltInAsset || flags.HasAll(SearchDocumentFlags.Nested | SearchDocumentFlags.Asset))
                        {
                            m_Object = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                        }
                        else
                        {
                            m_Object = AssetDatabase.LoadMainAssetAtGUID(gid.assetGUID);
                        }
                    }
                    return m_Object;
                }
            }

            public AssetMetaInfo(string path, GlobalObjectId gid, SearchDocumentFlags flags)
                : this(path, gid, flags, type: null)
            {
            }

            public AssetMetaInfo(string path, GlobalObjectId gid, SearchDocumentFlags flags, in Type type)
            {
                this.path = path;
                gidString = null;
                m_GID = gid;
                m_Source = null;
                m_Type = type;
                m_HasType = m_Type != null;
                m_Object = null;
                this.flags = flags;
            }

            public AssetMetaInfo(string path, string gid, SearchDocumentFlags flags)
                : this(path, gid, flags, type: null)
            {
            }

            public AssetMetaInfo(string path, string gid, SearchDocumentFlags flags, in Type type)
            {
                this.path = path;
                this.gidString = gid;
                m_GID = default;
                m_Source = null;
                m_Type = type;
                m_HasType = m_Type != null;
                m_Object = null;
                this.flags = flags;
            }

            public override string ToString()
            {
                return $"{path} ({m_GID})";
            }
        }

        internal const string filterId = "p:";
        internal const string type = "asset";
        private const string displayName = "Project";
        private static QueryEngine<string> m_QueryEngine;
        private static QueryEngine<string> queryEngine
        {
            get
            {
                if (m_QueryEngine == null)
                {
                    m_QueryEngine = new QueryEngine<string>(validateFilters: false);
                }
                return m_QueryEngine;
            }
        }

        private const string k_NoResultsLimitToggle = "noResultsLimit";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                priority = 25,
                filterId = filterId,
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Default | ShowDetailsOptions.Inspector | ShowDetailsOptions.DefaultGroup,
                supportsSyncViewSearch = true,
                isEnabledForContextualSearch = () => Utils.IsFocusedWindowTypeName("ProjectBrowser"),
                toObject = (item, type) => GetObject(item, type),
                toType = (item, constrainedType) => GetItemAssetType(item, constrainedType),
                toKey = (item) => GetDocumentKey(item),
                toInstanceId = (item) => GetItemInstanceId(item),
                fetchItems = (context, items, provider) => SearchAssets(context, provider),
                fetchLabel = (item, context) => FetchLabel(item),
                fetchDescription = (item, context) => FetchDescription(item),
                fetchThumbnail = (item, context) => FetchThumbnail(item),
                fetchPreview = (item, context, size, options) => FetchPreview(item, context, size, options),
                startDrag = (item, context) => StartDrag(item, context),
                trackSelection = (item, context) => EditorGUIUtility.PingObject(GetInstanceId(item)),
                fetchPropositions = (context, options) => FetchPropositions(context, options),
                fetchColumns = (context, items) => AssetSelectors.Enumerate(items)
            };
        }

        private static int GetItemInstanceId(in SearchItem item)
        {
            var info = GetInfo(item);
            if (info.gid.targetObjectId == 0)
                return AssetDatabase.GetMainAssetInstanceID(GetInfo(item).source);
            return GlobalObjectId.GlobalObjectIdentifierToInstanceIDSlow(info.gid);
        }

        private static ulong GetDocumentKey(SearchItem item)
        {
            return GetInfo(item).guid.GetHashCode64();
        }

        private static Texture2D FetchPreview(SearchItem item, SearchContext context, Vector2 size, FetchPreviewOptions options)
        {
            var info = GetInfo(item);
            if (info.gid.assetGUID == default)
                return null;

            if (item.preview && item.preview.width >= size.x && item.preview.height >= size.y)
                return item.preview;

            if (info.gid.identifierType == (int)IdentifierType.kSceneObject)
                return AssetDatabase.GetCachedIcon(info.source) as Texture2D;

            var objInstanceId = info.obj ? info.obj.GetInstanceID() : 0;
            var clientId = context.searchView != null ? context.searchView.GetViewId() : 0;

            if (info.gid.identifierType == (int)IdentifierType.kBuiltInAsset)
                return AssetPreview.GetAssetPreview(objInstanceId, clientId) ?? AssetPreview.GetMiniThumbnail(info.obj);

            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(info.gid);
            if (obj is GameObject go)
                return Utils.GetSceneObjectPreview(context, go, size, options, item.thumbnail);
            else if (obj && options.HasAny(FetchPreviewOptions.Normal))
            {
                var p = AssetPreview.GetAssetPreview(obj.GetInstanceID(), clientId);
                if (p)
                    return p;

                if (AssetPreview.IsLoadingAssetPreview(obj.GetInstanceID(), clientId))
                    return null;
            }

            if (obj && AssetDatabase.IsSubAsset(obj))
            {
                var p = Utils.GetAssetPreviewFromPath(context, obj, info.source, size, options);
                if (p) return p;

                if (info.type != null)
                {
                    p = SearchUtils.GetTypeIcon(info.type);
                    if (p) return p;
                }
            }

            return Utils.GetAssetPreviewFromPath(context, info.source, size, options);
        }

        private static Texture2D FetchThumbnail(in SearchItem item)
        {
            var info = GetInfo(item);

            if (item.thumbnail)
                return item.thumbnail;

            if (info.gid.identifierType == (int)IdentifierType.kBuiltInAsset)
                return AssetPreview.GetMiniThumbnail(info.obj);

            return SearchUtils.GetTypeIcon(info.type ?? typeof(GameObject));
        }

        private static string TrimLabel(in string label, in bool trim)
        {
            if (!trim)
                return label;
            var dp = label.LastIndexOfAny(Path.GetInvalidFileNameChars());
            if (dp > 0)
                return label.Substring(dp);
            return label;
        }

        private static string FetchLabel(SearchItem item)
        {
            var info = GetInfo(item);
            var displayCompact = IsDisplayCompact(item);

            if (!string.IsNullOrEmpty(item.label))
                return item.label;

            if (info.gid.identifierType == (int)IdentifierType.kBuiltInAsset && info.obj != null)
            {
                if (displayCompact)
                    return info.obj.name;
                return (item.label = $"{info.obj.name} ({info.obj.GetType()})");
            }

            if (info.flags.HasAny(SearchDocumentFlags.Object))
                return TrimLabel((item.label = info.path), displayCompact);
            item.label = Path.GetFileName(info.path);
            if (string.IsNullOrEmpty(item.label) && info.obj != null)
            {
                item.label = info.obj.name;
            }
            return item.label;
        }

        private static string FetchDescription(SearchItem item)
        {
            var info = GetInfo(item);

            if (info.gid.identifierType == (int)IdentifierType.kBuiltInAsset)
            {
                var desc = info.obj.ToString();
                if (string.IsNullOrEmpty(desc))
                    desc = info.obj.name;
                return desc;
            }

            if (IsDisplayCompact(item))
                return info.path;

            if (!string.IsNullOrEmpty(item.description))
                return item.description;

            if (info.flags.HasAny(SearchDocumentFlags.Asset))
                return (item.description = GetAssetDescription(info.source) ?? info.path);
            return (item.description = $"Source: {GetAssetDescription(info.source) ?? info.path}");
        }

        static bool IsDisplayCompact(in SearchItem item)
        {
            if (item.options.HasAny(SearchItemOptions.Compacted))
                return true;
            return item.context?.searchView?.displayMode == DisplayMode.Grid;
        }

        private static AssetMetaInfo GetInfo(in SearchItem item)
        {
            return (AssetMetaInfo)item.data;
        }

        private static GlobalObjectId GetGID(SearchItem item)
        {
            return GetInfo(item).gid;
        }

        private static int GetInstanceId(SearchItem item)
        {
            var gid = GetGID(item);
            return GlobalObjectId.GlobalObjectIdentifierToInstanceIDSlow(gid);
        }

        private static Type GetItemAssetType(in SearchItem item, in Type constrainedType)
        {
            var info = GetInfo(item);

            return info.type;
        }

        private static Object GetObject(SearchItem item)
        {
            return GetObject(item, typeof(UnityEngine.Object));
        }

        private static Object GetObject(SearchItem item, Type type)
        {
            var info = GetInfo(item);

            if (info.gid.identifierType == (int)IdentifierType.kBuiltInAsset)
                return info.obj;

            if (typeof(AssetImporter).IsAssignableFrom(type))
            {
                var importer = AssetImporter.GetAtPath(info.source);
                if (importer)
                    return importer;
            }

            if (info.flags.HasAny(SearchDocumentFlags.Asset))
            {
                if (info.flags.HasAny(SearchDocumentFlags.Nested))
                    return info.obj;
                var assetType = info.type;
                if (!type.IsAssignableFrom(assetType) && !(typeof(Component).IsAssignableFrom(type) && assetType == typeof(GameObject)))
                    return null;
                return AssetDatabase.LoadAssetAtPath(info.source, type);
            }

            return ToObjectType(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(info.gid), type);
        }

        private static Object ToObjectType(Object obj, Type type)
        {
            if (!obj)
                return null;

            if (type == null)
                return obj;
            var objType = obj.GetType();
            if (type.IsAssignableFrom(objType))
                return obj;

            if (obj is GameObject go && typeof(Component).IsAssignableFrom(type))
                return go.GetComponent(type);

            return null;
        }

        private static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (context.options.HasAny(SearchFlags.NoIndexing))
                return null;

            if (options.flags.HasAny(SearchPropositionFlags.QueryBuilder))
                return FetchQueryBuilderPropositions();

            // TODO: FIXME, should we show boxcollider.enabled AND enabled when we type "e"?
            var token = options.tokens[0];
            var ft = token.LastIndexOfAny(SearchUtils.KeywordsValueDelimiters);
            if (ft >= 0)
                token = token.Substring(0, ft);
            var dbs = SearchDatabase.EnumerateAll();
            return dbs.Where(db => !db.settings.options.disabled).SelectMany(db => db.index.GetKeywords()
                .Where(kw => kw.StartsWith(token, StringComparison.OrdinalIgnoreCase)))
                .Select(kw => new SearchProposition(category: null, label: kw));
        }

        static string[] s_DefaultFilters = new[] { "t", "a", "l", "prefab", "b", "is", "tag", "dir", "size", "ext", "age", "name", "ref", "tagstring" };
        private static IEnumerable<string> PopulateDefaultFilters()
        {
            return s_DefaultFilters;
        }

        private static IEnumerable<SearchProposition> FetchQueryBuilderPropositions()
        {
            foreach (var p in QueryAndOrBlock.BuiltInQueryBuilderPropositions())
                yield return p;
            foreach (var p in SearchUtils.FetchTypePropositions<Object>())
                yield return p;
            foreach (var p in FetchIndexPropositions())
                yield return p;

            foreach (var l in QueryListBlockAttribute.GetPropositions(typeof(QueryLabelBlock)))
                yield return l;
            foreach (var l in QueryListBlockAttribute.GetPropositions(typeof(QueryPrefabFilterBlock)))
                yield return l;
            foreach (var f in QueryListBlockAttribute.GetPropositions(typeof(QueryBundleFilterBlock)))
                yield return f;

            if (SearchDatabase.EnumerateAll().Any(db => db.index?.settings?.options.extended ?? false))
            {
                foreach (var f in QueryListBlockAttribute.GetPropositions(typeof(QueryIsFilterBlock)))
                    yield return f;
                foreach (var t in QueryListBlockAttribute.GetPropositions(typeof(QueryTagBlock)))
                    yield return t;
            }

            yield return new SearchProposition(category: "Options", "Fuzzy Matches", "+fuzzy",
                "Use fuzzy search to match object names", priority: 0, moveCursor: TextCursorPlacement.MoveAutoComplete, icon: Icons.toggles,
                color: QueryColors.toggle);
            yield return new SearchProposition(category: "Options", "No Results Limit", $"+{k_NoResultsLimitToggle}",
                "Do not cap asset provider results limit", priority: 0, moveCursor: TextCursorPlacement.MoveAutoComplete, icon: Icons.toggles,
                color: QueryColors.toggle);

            yield return new SearchProposition(category: "Filters", label: "Directory (Name)", replacement: "dir=\"folder name\"", help:"Assets in a specific folder",icon: Icons.quicksearch, color: QueryColors.filter);
            yield return new SearchProposition(category: "Filters", label: "File Size", replacement: "size>=8096", help: "Assets with a specific size in bytes", icon: Icons.quicksearch, color: QueryColors.filter);
            yield return new SearchProposition(category: "Filters", label: "File Extension", replacement: "ext:png", help:"Assets with a specific extension", icon: Icons.quicksearch, color: QueryColors.filter);
            yield return new SearchProposition(category: "Filters", label: "Age", replacement: "age>=1.5", help: "In days, when was the file last modified?", icon: Icons.quicksearch, color: QueryColors.filter);
            yield return new SearchProposition(category: "Filters", label: "Sub Asset", replacement: "is:subasset", help: "Yield nested assets (i.e. media from FBX files)", icon: Icons.quicksearch, color: QueryColors.filter);
            yield return new SearchProposition(category: "Filters", label: "Name", replacement: "name=", help: "Search asset by object name", icon: Icons.quicksearch, color: QueryColors.filter);

            var sceneIcon = Utils.LoadIcon("SceneAsset Icon");
            yield return new SearchProposition(category: null, "Reference", "ref=<$object:none,UnityEngine.Object$>", "Find all assets referencing a specific asset.", icon: sceneIcon, color: QueryColors.filter);
        }

        private static IEnumerable<SearchProposition> FetchIndexPropositions()
        {
            var dbs = SearchDatabase.EnumerateAll();
            foreach (var db in dbs.Where(db => !db.settings.options.disabled))
            {
                if (!db.loaded)
                    continue;

                yield return new SearchProposition(
                    category: "Area (Index)",
                    label: db.name,
                    replacement: $"a:{db.name}",
                    help: $"Search assets index by {db.name}.index",
                    color: QueryColors.type,
                    icon: Icons.quicksearch);

                foreach (var kw in db.index.GetKeywords())
                    yield return SearchUtils.CreateKeywordProposition(kw);
            }
        }

        private static void StartDrag(SearchItem item, SearchContext context)
        {
            if (context.selection.Count > 1)
            {
                var selectedObjects = context.selection.Select(i => GetObject(i));
                var paths = context.selection.Select(i => GetAssetPath(i)).ToArray();
                Utils.StartDrag(selectedObjects.ToArray(), paths, item.GetLabel(context, true));
            }
            else
                Utils.StartDrag(new[] { GetObject(item) }, new[] { GetAssetPath(item) }, item.GetLabel(context, true));
        }

        private static IEnumerator SearchAssets(SearchContext context, SearchProvider provider)
        {
            var searchQuery = context.searchQuery;
            if (string.IsNullOrEmpty(searchQuery))
                yield break;

            // Search by GUID
            var guidPath = AssetDatabase.GUIDToAssetPath(searchQuery);
            if (!string.IsNullOrEmpty(guidPath))
            {
                var info = new AssetMetaInfo(guidPath, GetGID(guidPath), SearchDocumentFlags.Asset);
                yield return provider.CreateItem(context, info.gid.ToString(), -1, $"{Path.GetFileName(guidPath)} ({searchQuery})", null, null, info);
            }

            if (searchQuery.StartsWith("GlobalObjectId", StringComparison.Ordinal))
            {
                if (GlobalObjectId.TryParse(searchQuery, out var gid))
                {
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                    var objPath = SearchUtils.GetObjectPath(obj, false);
                    var info = new AssetMetaInfo(objPath, gid,
                        gid.identifierType == (int)IdentifierType.kImportedAsset ? SearchDocumentFlags.Asset : SearchDocumentFlags.Nested | SearchDocumentFlags.Object);
                    yield return provider.CreateItem(context, gid.ToString(), -1, objPath, null, null, info);
                }
            }

            // Search indexes that are ready
            var useIndexing = !context.options.HasAny(SearchFlags.NoIndexing);
            if (!useIndexing || context.wantsMore)
            {
                // Perform a quick search on asset paths
                var findOptions = FindOptions.Words | FindOptions.Regex | FindOptions.Glob;
                if (context.options.HasAny(SearchFlags.Packages))
                    findOptions |= FindOptions.Packages;
                foreach (var e in FindProvider.Search(context, provider, findOptions))
                {
                    if (!e.valid)
                        yield return null;
                    else
                    {
                        yield return CreateItem("Files", context, SearchUtils.CreateGroupProvider(provider, "Files", 0, true),
                            null, e.source, 998 + e.score, SearchDocumentFlags.Asset);
                    }
                }
            }

            // Finally wait for indexes that are being built to end the search.
            if (useIndexing)
            {
                var dbs = SearchDatabase.EnumerateAll();
                foreach (var db in dbs)
                    yield return SearchIndexes(context.searchQuery, context, provider, db);
            }
        }

        public static GlobalObjectId GetGID(string assetPath)
        {
            return TaskEvaluatorManager.EvaluateMainThread(() =>
            {
                var assetInstanceId = Utils.GetMainAssetInstanceID(assetPath);
                return GlobalObjectId.GetGlobalObjectIdSlow(assetInstanceId);
            });
        }

        public static int GetResultLimit(string query)
        {
            var parsedQuery = queryEngine.ParseQuery(query);
            var resultsLimit = parsedQuery.toggles.Any(t => t.value.Equals(k_NoResultsLimitToggle, StringComparison.InvariantCultureIgnoreCase)) ? int.MaxValue : 2999;
            return resultsLimit;
        }

        private static IEnumerator SearchIndexes(string searchQuery, SearchContext context, SearchProvider provider, SearchDatabase db)
        {
            var cancelToken = context.sessions.cancelToken;
            if (!db.ready)
            {
                if (!Utils.IsRunningTests())
                {
                    var findOptions = FindOptions.Words | FindOptions.Regex | FindOptions.Glob;
                    foreach (var e in FindProvider.Search(searchQuery, db.settings.roots, context, provider, findOptions))
                    {
                        if (!e.valid)
                            yield return null;
                        else
                            yield return CreateItem("Files", context, SearchUtils.CreateGroupProvider(provider, "Files", 0, true),
                                null, e.source, 998 + e.score, SearchDocumentFlags.Asset);
                    }
                }

                while (!db.ready)
                {
                    if (!db || cancelToken.IsCancellationRequested || db.LoadingState == SearchDatabase.LoadState.Error || db.LoadingState == SearchDatabase.LoadState.Canceled)
                        yield break;
                    if (context.options.HasAny(SearchFlags.Synchronous))
                    {
                        Dispatcher.ProcessOne();
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }

            var query = searchQuery.ToLowerInvariant();
            var index = db.index;
            index.fetchDefaultFiler = PopulateDefaultFilters;
            var resultsLimit = GetResultLimit(query);

            var results = new System.Collections.Concurrent.ConcurrentBag<SearchResult>();
            var searchTask = System.Threading.Tasks.Task.Run(() =>
            {
                // Search index
                using var immutableScope = db.GetImmutableScope();
                foreach (var r in index.Search(query, context, provider, patternMatchLimit: resultsLimit))
                    results.Add(r);
            }, context.sessions.cancelToken);

            while (results.Count > 0 || !searchTask.IsCompleted || results.Count > 0)
            {
                while (results.TryTake(out var e))
                    yield return CreateItem(context, provider, db, e);

                if (!searchTask.Wait(0))
                    yield return null;
            }
        }

        private static SearchItem CreateItem(in SearchContext context, in SearchProvider provider, in SearchDatabase db, in SearchResult e)
        {
            var doc = db.index.GetDocument(e.index);
            if (string.IsNullOrEmpty(doc.id))
                return null;
            var score = ComputeSearchDocumentScore(context, doc, e.score);

            var flags = doc.flags;
            if (!IsProjectIndex(db))
                flags |= SearchDocumentFlags.Grouped;
            return CreateItem(db.name, context, provider, doc.id, doc.name, score, flags);
        }

        static int ComputeSearchDocumentScore(in SearchContext context, in SearchDocument doc, int score)
        {
            var docPath = doc.m_Name ?? (string.IsNullOrEmpty(doc.m_Source) ? null : Path.GetFileName(Utils.RemoveInvalidCharsFromPath(doc.m_Source, '_')));
            if (doc.m_Name != null)
                score <<= 2;
            if (!string.IsNullOrEmpty(docPath))
            {
                if (context.searchWords.Length == 0)
                {
                    score = docPath[0];
                }
                else
                {
                    foreach (var w in context.searchWords)
                    {
                        if (docPath.LastIndexOf(w, StringComparison.OrdinalIgnoreCase) != -1)
                            score = (score >> 1) + docPath.Length - w.Length;
                    }
                }
            }
            return score;
        }

        static bool IsProjectIndex(in SearchDatabase db)
        {
            if (string.IsNullOrEmpty(db.name))
                return true;
            return db.name.IndexOf("assets", StringComparison.OrdinalIgnoreCase) != -1 ||
                db.name.IndexOf("project", StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static SearchItem CreateItem(
            in string tag, in SearchContext context, SearchProvider provider,
            in string gid, in string path, in int itemScore,
            in SearchDocumentFlags flags)
        {
            return CreateItem(tag, context, provider, null, gid, path, itemScore, flags);
        }

        public static SearchItem CreateItem(
            in string tag, in SearchContext context, SearchProvider provider,
            in Type type, in string gid, in string path, in int itemScore,
            in SearchDocumentFlags flags)
        {
            string filename = null;
            if (context.options.HasAny(SearchFlags.Debug) && !string.IsNullOrEmpty(tag))
            {
                filename = Path.GetFileName(path);
                filename += $" ({tag}, {itemScore})";
            }

            if (flags.HasAny(SearchDocumentFlags.Grouped))
                provider = SearchUtils.CreateGroupProvider(provider, GetProviderGroupName(tag, path), provider.priority, cacheProvider: true);
            else if (flags.HasAny(SearchDocumentFlags.Object))
                provider = SearchUtils.CreateGroupProvider(provider, "Objects", provider.priority, cacheProvider: true);
            var info = new AssetMetaInfo(path, gid, flags, type);
            return provider.CreateItem(context, gid ?? info.gid.ToString(), itemScore, filename, null, null, info);
        }

        private static string GetProviderGroupName(string dbName, string path)
        {
            if (string.IsNullOrEmpty(path))
                return dbName;
            if (path.StartsWith("Packages/", StringComparison.Ordinal))
                return "Packages";
            return dbName;
        }

        [SearchSelector("path", provider: type)]
        public static string GetAssetPath(SearchItem item)
        {
            var info = GetInfo(item);
            return info.source;
        }

        public static string GetAssetPath(string id)
        {
            if (GlobalObjectId.TryParse(id, out var gid))
                return AssetDatabase.GUIDToAssetPath(gid.assetGUID);
            return AssetDatabase.GUIDToAssetPath(id);
        }

        public static string GetAssetPath(SearchResult result)
        {
            return GetAssetPath(result.id);
        }

        public static string GetAssetPath(SearchDocument doc)
        {
            return GetAssetPath(doc.id);
        }

        private static string GetAssetDescription(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return assetPath;
            try
            {
                var fi = new FileInfo(assetPath);
                if (!fi.Exists)
                    return $"File <i>{assetPath}</i> does not exist anymore.";
                return $"{assetPath} ({EditorUtility.FormatBytes(fi.Length)})";
            }
            catch
            {
                return null;
            }
        }

        private static void ReimportAssets(IEnumerable<SearchItem> items)
        {
            const ImportAssetOptions reimportAssetOptions =
                ImportAssetOptions.ForceUpdate |
                ImportAssetOptions.ImportRecursive |
                ImportAssetOptions.DontDownloadFromCacheServer |
                (ImportAssetOptions)(1 << 7); // AssetDatabase::kMayCancelImport

            foreach (var searchItem in items)
                AssetDatabase.ImportAsset(GetAssetPath(searchItem), reimportAssetOptions);
        }

        #region DocumentationDeleteAssets
        private static void DeleteAssets(IEnumerable<SearchItem> items)
        {
            var oldSelection = Selection.objects;
            SearchUtils.SelectMultipleItems(items, pingSelection: false);
            // We call ProjectBrowser.DeleteSelectedAssets for the confirmation popup.
            ProjectBrowser.DeleteSelectedAssets(true);
            Selection.objects = oldSelection;
        }

        #endregion

        // We have our own OpenPropertyEditorsOnSelection so we don't have to worry about global selection
        private static void OpenPropertyEditorsOnSelection(IEnumerable<SearchItem> items)
        {
            var objs = items.Select(i => i.ToObject()).Where(o => o).ToArray();
            if (objs.Length == 0)
                return;
            if (objs.Length == 1)
            {
                Utils.OpenPropertyEditor(objs[0]);
            }
            else if (objs.Length > 0)
            {
                var firstPropertyEditor = PropertyEditor.OpenPropertyEditor(objs[0]);
                EditorApplication.delayCall += () =>
                {
                    var dock = firstPropertyEditor.m_Parent as DockArea;
                    if (dock == null)
                        return;
                    for (var i = 1; i < objs.Length; ++i)
                        dock.AddTab(PropertyEditor.OpenPropertyEditor(objs[i], false));
                };
            }
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> CreateActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "select", null, "Select")
                {
                    handler = (item) => SelectItem(item),
                    execute = (items) => SearchUtils.SelectMultipleItems(items, focusProjectBrowser: true)
                },
                new SearchAction(type, "open", null, "Open", OpenItem) { enabled = items => items.Count == 1 },
                new SearchAction(type, "reimport", null, "Reimport", ReimportAssets) { enabled = items => items.Count == 1 },
                new SearchAction(type, "add_scene", null, "Add scene")
                {
                    // Only works in single selection and adds a scene to the current hierarchy.
                    enabled = (items) => CanAddScene(items),
                    handler = (item) => SceneManagement.EditorSceneManager.OpenScene(GetAssetPath(item), SceneManagement.OpenSceneMode.Additive)
                },
                new SearchAction(type, "reveal", null, Utils.GetRevealInFinderLabel(), item => EditorUtility.RevealInFinder(GetAssetPath(item))),
                new SearchAction(type, "delete", null, "Delete", DeleteAssets),
                new SearchAction(type, "copy_path", null, "Copy Path")
                {
                    enabled = items => items.Count >= 1,
                    execute = items =>
                    {
                        var paths = items.Select(GetAssetPath).Where(path => !string.IsNullOrEmpty(path)).ToArray();
                        if (paths.Length == 1)
                        {
                            EditorGUIUtility.systemCopyBuffer = paths[0];
                            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, items[0].ToObject(), paths[0]);
                        }
                        else if (paths.Length > 1)
                        {
                            var pathsStr = string.Join(",", paths);
                            EditorGUIUtility.systemCopyBuffer = pathsStr;
                            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, pathsStr);
                        }
                    }
                },
                new SearchAction(type, "copy_guid", null, "Copy GUID")
                {
                    enabled = items => items.Count >= 1,
                    execute = items =>
                    {
                        var guids = items.Select(item => AssetDatabase.AssetPathToGUID(GetAssetPath(item))).Where(path => !string.IsNullOrEmpty(path)).ToArray();
                        if (guids.Length == 1)
                        {
                            EditorGUIUtility.systemCopyBuffer = guids[0];
                            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, items[0].ToObject(), guids[0]);
                        }
                        else if (guids.Length > 1)
                        {
                            var guidsStr = string.Join(",", guids);
                            EditorGUIUtility.systemCopyBuffer = guidsStr;
                            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, guidsStr);
                        }
                    }
                },
                new SearchAction(type, "properties", null, "Properties", OpenPropertyEditorsOnSelection)
                {
                }
            };
        }

        private static void SelectItem(SearchItem item)
        {
            var info = GetInfo(item);
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(info.gid);
            if (obj)
            {
                EditorApplication.delayCall += () =>
                {
                    if (!SelectObjectbyId(info.gid))
                    {
                        if (EditorUtility.DisplayDialog("Container scene is not opened", $"Do you want to open container scene {info.source}?", "Yes", "No"))
                            OpenItem(item);
                    }
                };
            }
            else
                Utils.FrameAssetFromPath(GetAssetPath(item));
            item.preview = null;
        }

        private static void OpenItem(SearchItem item)
        {
            var info = GetInfo(item);
            if (info.gid.identifierType == (int)IdentifierType.kSceneObject)
            {
                var containerAsset = AssetDatabase.LoadAssetAtPath<Object>(info.source);
                if (containerAsset != null)
                {
                    AssetDatabase.OpenAsset(containerAsset);
                    EditorApplication.delayCall += () => SelectObjectbyId(info.gid);
                }
            }

            var asset = GetObject(item);
            if (asset == null || !AssetDatabase.OpenAsset(asset))
                EditorUtility.OpenWithDefaultApp(GetAssetPath(item));
        }

        private static bool SelectObjectbyId(GlobalObjectId gid)
        {
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            if (obj)
            {
                Utils.SelectObject(obj);
                return true;
            }
            return false;
        }

        private static bool CanAddScene(IReadOnlyCollection<SearchItem> items)
        {
            if (items.Count != 1)
                return false;
            var singleItem = items.Last();
            var info = GetInfo(singleItem);
            if (info.gid.identifierType != (int)IdentifierType.kImportedAsset)
                return false;
            return info.path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        }

        [Shortcut("Help/Search/Assets")]
        internal static void PopQuickSearch()
        {
            SearchUtils.OpenWithContextualProviders(type, FindProvider.providerId);
        }

        [SearchTemplate(description = "Find all textures", providerId = type)] internal static string ST1() => @"t:texture";
        [SearchTemplate(description = "Search current folder", providerId = type)] internal static string ST2() => $"dir=\"{Path.GetFileName(ProjectWindowUtil.GetActiveFolderPath())}\" ";
    }
}

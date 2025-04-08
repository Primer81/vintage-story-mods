using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace Vintagestory.Common;

public class ModAssemblyLoader : IDisposable
{
	[CompilerGenerated]
	private sealed class _003CGetAssemblySearchPaths_003Ed__6 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public ModAssemblyLoader _003C_003E4__this;

		private IEnumerator<string> _003C_003E7__wrap1;

		string IEnumerator<string>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CGetAssemblySearchPaths_003Ed__6(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			switch (_003C_003E1__state)
			{
			case -3:
			case 3:
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
				break;
			case -4:
			case 4:
				try
				{
				}
				finally
				{
					_003C_003Em__Finally2();
				}
				break;
			}
			_003C_003E7__wrap1 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = _003C_003E1__state;
				ModAssemblyLoader modAssemblyLoader = _003C_003E4__this;
				IEnumerable<string> folderPaths;
				switch (num)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003C_003E2__current = AppDomain.CurrentDomain.BaseDirectory;
					_003C_003E1__state = 1;
					return true;
				case 1:
					_003C_003E1__state = -1;
					_003C_003E2__current = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib");
					_003C_003E1__state = 2;
					return true;
				case 2:
					_003C_003E1__state = -1;
					_003C_003E7__wrap1 = modAssemblyLoader.modSearchPaths.GetEnumerator();
					_003C_003E1__state = -3;
					goto IL_00d0;
				case 3:
					_003C_003E1__state = -3;
					goto IL_00d0;
				case 4:
					{
						_003C_003E1__state = -4;
						break;
					}
					IL_00d0:
					if (_003C_003E7__wrap1.MoveNext())
					{
						string modsPath = _003C_003E7__wrap1.Current;
						_003C_003E2__current = modsPath;
						_003C_003E1__state = 3;
						return true;
					}
					_003C_003Em__Finally1();
					_003C_003E7__wrap1 = null;
					folderPaths = from mod in modAssemblyLoader.mods
						select mod.FolderPath into path
						where path != null
						select path;
					_003C_003E7__wrap1 = folderPaths.GetEnumerator();
					_003C_003E1__state = -4;
					break;
				}
				if (_003C_003E7__wrap1.MoveNext())
				{
					string path2 = _003C_003E7__wrap1.Current;
					_003C_003E2__current = path2;
					_003C_003E1__state = 4;
					return true;
				}
				_003C_003Em__Finally2();
				_003C_003E7__wrap1 = null;
				return false;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap1 != null)
			{
				_003C_003E7__wrap1.Dispose();
			}
		}

		private void _003C_003Em__Finally2()
		{
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap1 != null)
			{
				_003C_003E7__wrap1.Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			_003CGetAssemblySearchPaths_003Ed__6 result;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				result = this;
			}
			else
			{
				result = new _003CGetAssemblySearchPaths_003Ed__6(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			return result;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}
	}

	private readonly IReadOnlyCollection<string> modSearchPaths;

	private readonly IReadOnlyCollection<ModContainer> mods;

	public ModAssemblyLoader(IReadOnlyCollection<string> modSearchPaths, IReadOnlyCollection<ModContainer> mods)
	{
		this.modSearchPaths = modSearchPaths;
		this.mods = mods;
		AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveHandler;
	}

	public void Dispose()
	{
		AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolveHandler;
	}

	public Assembly LoadFrom(string path)
	{
		return Assembly.UnsafeLoadFrom(path);
	}

	public AssemblyDefinition LoadAssemblyDefinition(string path)
	{
		return AssemblyDefinition.ReadAssembly(path);
	}

	[IteratorStateMachine(typeof(_003CGetAssemblySearchPaths_003Ed__6))]
	private IEnumerable<string> GetAssemblySearchPaths()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetAssemblySearchPaths_003Ed__6(-2)
		{
			_003C_003E4__this = this
		};
	}

	private Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
	{
		return (from searchPath in GetAssemblySearchPaths()
			select Path.Combine(searchPath, args.Name + ".dll") into assemblyPath
			where File.Exists(assemblyPath)
			select LoadFrom(assemblyPath)).FirstOrDefault();
	}
}

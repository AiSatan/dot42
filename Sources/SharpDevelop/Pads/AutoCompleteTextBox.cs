﻿

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;

namespace Dot42.SharpDevelop.Pads
{
	/// <summary>
	/// Description of AutoCompleteTextBox.
	/// </summary>
	public class AutoCompleteTextBox : UserControl
	{
		TextEditor editor;
		ITextEditor editorAdapter;
		
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof(string), typeof(AutoCompleteTextBox),
			                            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TextChanged));
		
		public static readonly DependencyProperty IsEditableProperty =
			DependencyProperty.Register("IsEditable", typeof(bool), typeof(AutoCompleteTextBox),
			                            new FrameworkPropertyMetadata(true, IsEditableChanged));	
		
		public string Text {
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}
		
		public bool IsEditable {
			get { return (bool)GetValue(IsEditableProperty); }
			set { SetValue(IsEditableProperty, value); }
		}
		
		static void TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((AutoCompleteTextBox)d).editor.Text = (string)e.NewValue;
		}
		
		static void IsEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((AutoCompleteTextBox)d).editor.IsReadOnly = !(bool)e.NewValue;
		}
		
		public AutoCompleteTextBox()
		{
			object tmp;
#if SD5
			this.editorAdapter = SD.EditorControlService.CreateEditor(out tmp);
#else
		#fail
			this.editorAdapter = WorkbenchSingleton.c SD.EditorControlService.CreateEditor(out tmp);
#endif
			this.editor = (TextEditor)tmp;
			
			this.editor.Background = Brushes.Transparent;
			this.editor.ClearValue(TextEditor.FontFamilyProperty);
			this.editor.ClearValue(TextEditor.FontSizeProperty);
			this.editor.ShowLineNumbers = false;
			this.editor.WordWrap = false;
			this.editor.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
			this.editor.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
			this.editor.TextArea.GotKeyboardFocus += delegate {
				this.Background = Brushes.White;
			};
			this.editor.TextArea.LostKeyboardFocus += delegate {
				this.Background = Brushes.Transparent;
				this.Text = this.editor.Text;
				this.editor.Select(0, 0);
			};
			this.editor.TextArea.PreviewKeyDown += editor_TextArea_PreviewKeyDown;
			this.editor.TextArea.TextEntered += editor_TextArea_TextEntered;
			
			this.Content = this.editor.TextArea;
		}
		
		void editor_TextArea_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return || e.Key == Key.Escape) {
				if (e.Key == Key.Return)
					this.Text = this.editor.Text;
				
				e.Handled = true;
			}
		}
		
		void editor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
		{
		}
		
/*		private void ShowDotCompletion(StackFrame frame, string currentText)
		{
			string language = ProjectService.CurrentProject == null ? "C#" : ProjectService.CurrentProject.Language;
			#warning reimplement this!
//			NRefactoryResolver resolver = new NRefactoryResolver(LanguageProperties.GetLanguage(language));
//			
//			var seg = frame.NextStatement;
//			
//			var expressionFinder = ParserService.GetExpressionFinder(seg.Filename);
//			var info = ParserService.GetParseInformation(seg.Filename);
//			
//			string text = ParserService.GetParseableFileContent(seg.Filename).Text;
//			
//			int currentOffset = this.editor.CaretOffset;
//			
//			var expr = expressionFinder.FindExpression(currentText, currentOffset);
//			
//			expr.Region = new DomRegion(seg.StartLine, seg.StartColumn, seg.EndLine, seg.EndColumn);
//			
//			var rr = resolver.Resolve(expr, info, text);
//			
//			if (rr != null) {
//				editorAdapter.ShowCompletionWindow(new DotCodeCompletionItemProvider().GenerateCompletionListForResolveResult(rr, expr.Context));
//			}
		}
		*/
	}
}

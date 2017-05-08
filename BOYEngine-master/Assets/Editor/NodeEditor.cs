using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;
using System.IO;

public class NodeEditor : EditorWindow {
	
	Spool loadedStory;
	string folderPath;
	PrefLayout prefLayout;
	List<StitchNode> spoolNodes = new List<StitchNode> ();
	
	// Editor Settings
	float editSpacing;
	float editPadding;
	float editContentHeight;
	float editColWidth;
	float editRow1Height;
	float editRow2Height;
	float editCol1;
	float editCol2;
	float editCol3;
	float editRow1;
	float editRow2;
	Texture2D editBG;
	Rect editRect;
	Vector2 editScroll1;
	Vector2 editScroll2;
	Vector2 editScroll3;
	Texture2D backgroundSprite;
	float bgScale;
	int bgWidth;
	int bgHeight;


	public static int editStitch = -1;
	public static int attachingYarnID = -1;

	[MenuItem("Node Editor/ Spool Editor")]
	public static void showWindow()
	{

		NodeEditor window = (NodeEditor) EditorWindow.GetWindow (typeof (NodeEditor));
		window.minSize = new Vector2 (1000, 800);

	}


	public void OnGUI()
	{
		
		EditorGUI.BeginChangeCheck ();
		loadedStory = EditorGUILayout.ObjectField (loadedStory, typeof(Spool), false) as Spool;

		if (loadedStory == null) {
			return;
		}

		// If the object field is given a spool, create a list of stitch nodes
		if (EditorGUI.EndChangeCheck ()) {
			
			string[] storyPath = AssetDatabase.GetAssetPath (loadedStory).Split ('/');
			folderPath = string.Join ("/", storyPath, 0, storyPath.Length - 1) + "/";

			EditorUtility.SetDirty (loadedStory);

			bool hasPreference = false;
			prefLayout = ScriptableObject.CreateInstance <PrefLayout> ();
			if (AssetDatabase.LoadAssetAtPath <PrefLayout> (folderPath + "Pref" + loadedStory.storyName.Replace (" ", "_") + ".asset") != null) {
				prefLayout = AssetDatabase.LoadAssetAtPath <PrefLayout> (folderPath + "Pref" + loadedStory.storyName.Replace (" ", "_") + ".asset");
				hasPreference = true;
			} else {
				AssetDatabase.CreateAsset (prefLayout, folderPath + "Pref" + loadedStory.storyName.Replace (" ", "_") + ".asset");
			}
			EditorUtility.SetDirty (prefLayout);


			spoolNodes.Clear ();
			for (int i = 0; i < loadedStory.stitchCollection.Length; i++) {
				
				EditorUtility.SetDirty (loadedStory.stitchCollection [i]);
				
				float vertOffset = 60;
				if (i%2 != 0) {
					vertOffset = 180;
				}

				StitchNode newStitchNode = ScriptableObject.CreateInstance <StitchNode> ();

				Rect nodeRect = new Rect (30 + i * 150, vertOffset, 120, 120);

				if (hasPreference) {
					if (prefLayout.nodeRects.Length > i) {
						nodeRect = prefLayout.nodeRects [i];
					}
				}

				newStitchNode.rect = nodeRect;
				newStitchNode.stitch = loadedStory.stitchCollection [i];
				newStitchNode.id = i;
				newStitchNode.masterWindow = this;

				spoolNodes.Add (newStitchNode);

			}

			UpdateNodeIDs ();

		}

		prefLayout.nodeRects = new Rect[spoolNodes.Count];
		for (int i = 0; i < spoolNodes.Count; i++) {
			prefLayout.nodeRects [i] = spoolNodes [i].rect;
		}

		// Draw Lines
		for (int i = 0; i < spoolNodes.Count; i++) {
			
			for (int j = 0; j < spoolNodes [i].stitch.yarns.Length; j++) {
				
				Color color = Color.yellow;

				if (spoolNodes [i].stitch.yarns [j].choiceStitch != null) {
					StitchNode startingNode = spoolNodes [i];
					StitchNode endingNode = spoolNodes [spoolNodes [i].stitch.yarns [j].choiceStitch.stitchID];

					if (startingNode.stitch.status == Stitch.stitchStatus.auto) {
						color = Color.green;
					}

					DrawNodeCurve(startingNode.rect, endingNode.rect, j, color);
				}

			}

		}
			
		GUILayout.Label (loadedStory.storyName);

		EditorGUILayout.BeginHorizontal ();

		// Add Stitch button
		if (GUILayout.Button("Create New Stitch", GUILayout.Width (140))) {
			
			Stitch newStitch = ScriptableObject.CreateInstance <Stitch> ();
			EditorUtility.SetDirty (newStitch);
			CreateNewStitch (newStitch);
			string filePath = folderPath + loadedStory.storyName.Replace (" ", "_") + "Stitch.asset";
			filePath = AssetDatabase.GenerateUniqueAssetPath (filePath);
			AssetDatabase.CreateAsset (newStitch, filePath);
			StitchNode newStitchNode = ScriptableObject.CreateInstance <StitchNode> ();
			newStitchNode.rect = new Rect (40, 70, 120, 120);
			newStitchNode.stitch = newStitch;
			newStitchNode.id = spoolNodes.Count;
			newStitchNode.masterWindow = this;

			spoolNodes.Add (newStitchNode);

			Stitch[] tempCollection = new Stitch[spoolNodes.Count];
			loadedStory.stitchCollection.CopyTo (tempCollection, 0);
			loadedStory.stitchCollection = tempCollection;
			loadedStory.stitchCollection [spoolNodes.Count - 1] = newStitch;


		}

		// Add Stitch Input
		Stitch addedStitch = ScriptableObject.CreateInstance <Stitch> ();
		GUILayout.Space (50f);
		EditorGUI.BeginChangeCheck ();
		addedStitch = EditorGUILayout.ObjectField (addedStitch, typeof(Stitch), false, GUILayout.Width (120)) as Stitch;
		if (EditorGUI.EndChangeCheck ()) {
			bool noMatches = true;
			foreach (Stitch s in loadedStory.stitchCollection) {
				if (AssetDatabase.GetAssetPath (s) == AssetDatabase.GetAssetPath (addedStitch)) {
					noMatches = false;
				}
			}
			if (noMatches) {
				EditorUtility.SetDirty (addedStitch);

				StitchNode newStitchNode = ScriptableObject.CreateInstance <StitchNode> ();
				newStitchNode.rect = new Rect (40, 70, 120, 120);
				newStitchNode.stitch = addedStitch;
				newStitchNode.id = spoolNodes.Count;
				newStitchNode.masterWindow = this;
			
				spoolNodes.Add (newStitchNode);
			
				Stitch[] tempCollection = new Stitch[spoolNodes.Count];
				loadedStory.stitchCollection.CopyTo (tempCollection, 0);
				loadedStory.stitchCollection = tempCollection;
				loadedStory.stitchCollection [spoolNodes.Count - 1] = addedStitch;
			
				UpdateNodeIDs ();
			}

		}

		EditorGUILayout.LabelField ("Drag and Drop", GUILayout.Width (100));

		EditorGUILayout.EndHorizontal ();

		// Draw the node windows
		BeginWindows();
		for(int i = 0; i < spoolNodes.Count; i++) {

			// Color end window

			spoolNodes [i].rect = GUI.Window(i, spoolNodes [i].rect, spoolNodes [i].DrawGUI, spoolNodes [i].stitch.stitchName);

			spoolNodes [i].rect = ClampRect (spoolNodes [i].rect);

		}
		EndWindows();


		// Setup the Editor
		editSpacing = 10f;
		editPadding = 2f;
		editContentHeight = 16f;
		editColWidth = (position.width - editSpacing * 4) / 3;
		editRow1Height = editContentHeight * 2 + editPadding * 3;
		editRow2Height = editContentHeight * 10 + editPadding * 11;
		editCol1 = editSpacing;
		editCol2 = editSpacing * 2 + editColWidth;
		editCol3 = editSpacing * 3 + editColWidth * 2;
		editRow1 = position.height - (editSpacing * 2 + editRow1Height + editRow2Height);
		editRow2 = position.height - (editSpacing + editRow2Height);
		editBG = (Texture2D)Resources.Load ("blue", typeof(Texture2D)) as Texture2D;
//		editBG = EditorGUIUtility.whiteTexture as Texture2D;
		
		editRect = new Rect (editCol1, editRow1, editColWidth, editRow1Height);
		GUI.DrawTexture (editRect, editBG);
		editRect = new Rect (editCol1, editRow2, editColWidth, editRow2Height);
		GUI.DrawTexture (editRect, editBG);
		editRect = new Rect (editCol2, editRow1, editColWidth, editRow1Height);
		GUI.DrawTexture (editRect, editBG);
		editRect = new Rect (editCol2, editRow2, editColWidth, editRow2Height);
		GUI.DrawTexture (editRect, editBG);
		editRect = new Rect (editCol3, editRow1, editColWidth, editRow1Height);
		GUI.DrawTexture (editRect, editBG);
		editRect = new Rect (editCol3, editRow2, editColWidth, editRow2Height);
		GUI.DrawTexture (editRect, editBG);

		// Display if a stitch is selected to edit
		if (editStitch > -1 && editStitch < spoolNodes.Count) {

			// Row 1 x Column 1
			GUILayout.BeginArea (new Rect (editCol1 + editPadding, editRow1 + editPadding, editColWidth - editPadding*2, editRow1Height - editPadding*2));
			spoolNodes [editStitch].stitch.stitchName = EditorGUILayout.TextField ("Stitch Name", spoolNodes [editStitch].stitch.stitchName);
			EditorGUILayout.BeginHorizontal ();
			float fs = (editColWidth - editPadding) / 5; // one fourth the spacing of the column
			EditorGUILayout.LabelField ("Status", GUILayout.Width (fs));
			spoolNodes [editStitch].stitch.status = (Stitch.stitchStatus) EditorGUILayout.EnumPopup (spoolNodes [editStitch].stitch.status, GUILayout.Width (fs));
			GUILayout.FlexibleSpace ();
			EditorGUILayout.LabelField ("Stitch ID", GUILayout.Width (fs));
			EditorGUILayout.LabelField (spoolNodes [editStitch].stitch.stitchID.ToString (), GUILayout.Width (fs));
			EditorGUILayout.EndHorizontal ();
			GUILayout.EndArea ();
			
			// Row 1 x Column 2
			GUILayout.BeginArea (new Rect (editCol2 + editPadding, editRow1 + editPadding, editColWidth - editPadding*2, editRow1Height - editPadding*2));
			GUILayout.Label ("Summary");
			spoolNodes [editStitch].stitch.summary = EditorGUILayout.TextField (spoolNodes [editStitch].stitch.summary);
			GUILayout.EndArea ();

			// Row 1 x Column 3
			GUILayout.BeginArea (new Rect (editCol3 + editPadding, editRow1 + editPadding, editColWidth - editPadding*2, editRow1Height - editPadding*2));
			EditorGUILayout.BeginVertical ();
			GUILayout.FlexibleSpace ();
			spoolNodes [editStitch].stitch.background = EditorGUILayout.ObjectField (spoolNodes [editStitch].stitch.background, typeof(Sprite), false, GUILayout.Width (editColWidth - editPadding * 3 - bgWidth)) as Sprite;
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndVertical ();
			if (spoolNodes [editStitch].stitch.background != null) {
				backgroundSprite = SpriteUtility.GetSpriteTexture (spoolNodes [editStitch].stitch.background, false);
			} else {
				backgroundSprite = EditorGUIUtility.whiteTexture as Texture2D;
			}
			bgScale = (editContentHeight * 2 + editPadding) / backgroundSprite.height;
			bgWidth = (int)(backgroundSprite.width * bgScale);
			bgHeight = (int)(backgroundSprite.height * bgScale);
			Rect bgRect = new Rect (editColWidth - editPadding*2 - bgWidth, 0, bgWidth, bgHeight);
			GUI.DrawTexture (bgRect, backgroundSprite );
			GUILayout.EndArea ();

			// Row 2 x Column 1
			GUILayout.BeginArea (new Rect (editCol1 + editPadding, editRow2 + editPadding, editColWidth - editPadding*2, editRow2Height - editPadding*2));
			editScroll1 = EditorGUILayout.BeginScrollView (editScroll1);
			SerializedObject so = new SerializedObject (spoolNodes [editStitch].stitch);
			so.Update ();
			SerializedProperty preformer = so.FindProperty ("performers");
			EditorGUILayout.PropertyField (preformer, true);
			so.ApplyModifiedProperties ();
			EditorGUILayout.EndScrollView ();
			GUILayout.EndArea ();

			// Row 2 x Column 2
			GUILayout.BeginArea (new Rect (editCol2 + editPadding, editRow2 + editPadding, editColWidth - editPadding*2, editRow2Height - editPadding*2));
			editScroll2 = EditorGUILayout.BeginScrollView (editScroll2);
			so = new SerializedObject (spoolNodes [editStitch].stitch);
			so.Update ();
			SerializedProperty dialog = so.FindProperty ("dialogs");
			EditorGUILayout.PropertyField (dialog, true);
			so.ApplyModifiedProperties ();
			EditorGUILayout.EndScrollView ();
			GUILayout.EndArea ();

			// Row 2 x Column 3
			GUILayout.BeginArea (new Rect (editCol3 + editPadding, editRow2 + editPadding, editColWidth - editPadding*2, editRow2Height - editPadding*2));
			editScroll3 = EditorGUILayout.BeginScrollView (editScroll3);
			so = new SerializedObject (spoolNodes [editStitch].stitch);
			so.Update ();
			SerializedProperty yarns = so.FindProperty ("yarns");
			EditorGUILayout.PropertyField (yarns, true);
			so.ApplyModifiedProperties ();
			EditorGUILayout.EndScrollView ();
			GUILayout.EndArea ();

		}

	}


	// Draw the lines between the nodes
	void DrawNodeCurve(Rect start, Rect end, int index, Color color) {
		
		Vector3 startPos = new Vector3(start.x + start.width, start.y + (60)+10 + 18*index, 0);
		Vector3 endPos = new Vector3(end.x, end.y + (end.height / 2)+ 10, 0);
		Vector3 startTan = startPos + Vector3.right * 100;
		Vector3 endTan = endPos + Vector3.left * 100;

		Handles.DrawBezier(startPos, endPos, startTan, endTan, color, null, 5);

	}


	// Give a new Stitch default values
	void CreateNewStitch (Stitch s) {
		
		s.stitchID = spoolNodes.Count;
		s.stitchName = loadedStory.storyName + " " + (spoolNodes.Count+1).ToString ();
		s.summary = "Input Summary";
		//s.background;   
		s.performers = new Performer[0];
		s.dialogs = new Dialog[0];
		s.yarns = new Yarn[0];
		s.status = Stitch.stitchStatus.regular;

	}


	// Remove a node and all of it's references.
	public void RemoveNode(int id) {

		spoolNodes [id].stitch.yarns = new Yarn[0];
		
		for (int i = 0; i < spoolNodes.Count; i++) {

			if (spoolNodes [i].stitch.yarns.Length > 0) {
				List <Yarn> yarnList = new List <Yarn> (spoolNodes [i].stitch.yarns);
				yarnList.RemoveAll (y => y.choiceStitch != null && y.choiceStitch.stitchID == id);
				spoolNodes [i].stitch.yarns = yarnList.ToArray ();
			}

		}

		List <Stitch> stitchList = new List <Stitch> (loadedStory.stitchCollection);
		stitchList.RemoveAll (s => s.stitchID == id);
		loadedStory.stitchCollection = stitchList.ToArray ();

		spoolNodes.RemoveAt(id);
		UpdateNodeIDs();

	}


	public void UpdateNodeIDs() {

		for (int i = 0; i < loadedStory.stitchCollection.Length; i++) {
			loadedStory.stitchCollection [i].stitchID = i;
		}

		for (int i = 0; i < spoolNodes.Count; i++) {
			spoolNodes[i].id = i;
		}

	}


	public void BeginAttachment(int winID) {
		
		attachingYarnID = winID;

	}

	public void EndAttachment(int winID) {
		
		if (attachingYarnID > -1) {

			Yarn newYarn = new Yarn ();
			newYarn.choiceStitch = loadedStory.stitchCollection [winID];
			newYarn.choiceString = "Go to " + (winID+1) + "!";

			Yarn[] tempYarns = new Yarn[spoolNodes [attachingYarnID].stitch.yarns.Length + 1];
			spoolNodes [attachingYarnID].stitch.yarns.CopyTo (tempYarns, 0);
			spoolNodes [attachingYarnID].stitch.yarns = tempYarns;
			spoolNodes [attachingYarnID].stitch.yarns [spoolNodes [attachingYarnID].stitch.yarns.Length - 1] = newYarn;

		}

		attachingYarnID = -1;

	}


	public Rect ClampRect (Rect rect) {

		float topBoarder = 60f;
		float bottomBoarder = editSpacing*3 + editRow1Height + editRow2Height;
		float sideBoarder = 15f;

		Rect clampedRect = rect;

		clampedRect.x = Mathf.Clamp (rect.x, sideBoarder, position.width - rect.width - sideBoarder);
		clampedRect.y = Mathf.Clamp (rect.y, topBoarder, position.height - rect.height - bottomBoarder);

		return clampedRect;

	}

}

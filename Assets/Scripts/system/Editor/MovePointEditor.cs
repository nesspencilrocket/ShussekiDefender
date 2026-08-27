using UnityEngine;
using UnityEditor;

//https://docs.unity3d.com/Manual/editor-CustomEditors.html
//CustomEditor�����́A
//�ǂ̃R���|�[�l���g�̃G�f�B�^�Ƃ��ē��삳���邩��Unity�ɒm�点�܂��B
[CustomEditor(typeof(MovePoint))]
public class MovePointEditor : Editor
{
    //�ϐ��Ɋi�[����
    //�ΏۂƂȂ�MovePoint���̂��擾
    //Editor�N���X���́utarget�v�Ƃ����ϐ��������S���Ă��܂��B
    //target��object�^�Ȃ̂ŕ����Ⴂ�܂��B
    //������as MovePoint��MovePoint�^�ɕϊ����Ă܂�
    MovePoint MovePoint => target as MovePoint;


    private void OnSceneGUI()
    {
        //�F���w��
        Handles.color = Color.green;

        //�J��Ԃ�����
        for (int i = 0; i < MovePoint.points.Length; i++)
        {
            //EndChangeCheck�Ƃ̊ԂŃV�[�����ł̕ω����Ȃ��̂��m�F����
            EditorGUI.BeginChangeCheck();

            //�|�C���g�̈ʒu���擾
            Vector3 currentWaypoint = MovePoint.points[i];

            //�n���h���𐶐����ĕϐ��Ɋi�[����
            //(snap�̓X�i�b�v�ړ�����ۂ̈ړ��ʁF
            //�X�i�b�v�ړ���Ctrl�����Ȃ���ړ�������Ƃł���)
            var fmh_35_75_638962527771688206 = Quaternion.identity; Vector3 newWaypoint = Handles.FreeMoveHandle(currentWaypoint, 0.7f,
                new Vector3(0.3f, 0.3f, 0.3f), Handles.SphereHandleCap);


            //�n���h���ԍ��̐��� GUIStyle��GUI�̐ݒ�
            GUIStyle textStyle = new GUIStyle();
            textStyle.fontSize = 20;
            textStyle.normal.textColor = Color.red;
            Vector3 textPos = Vector3.down * 0.35f + Vector3.right * 0.35f;

            //���x���̔����ʒu�A�\�����e�A�\��GUI�̐ݒ�
            Handles.Label(MovePoint.points[i] + textPos, $"{i + 1}", textStyle);

            EditorGUI.EndChangeCheck();


            //�����ς���Ă�����
            if (EditorGUI.EndChangeCheck())
            {

                //�ۑ�:���ꂵ�Ȃ���Ctrl+Z�Ŗ߂����Ƃ��ł��Ȃ�
                Undo.RecordObject(target, "Move");

                //���������n���h���ʒu
                MovePoint.points[i] = newWaypoint;
            }
        }
    }

}

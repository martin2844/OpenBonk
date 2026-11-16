import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import patch


path = Path(__file__).resolve().parents[1] / 'audit-inline-mesh-payloads.py'
spec = importlib.util.spec_from_file_location('mesh_audit', path)
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class Archive:
    def __init__(self, data):
        self.data = data

    def read(self, _part):
        return self.data


class MeshAuditTest(unittest.TestCase):
    def test_inline_buffers_and_streamed_vertices_are_reported(self):
        part = SimpleNamespace(filename='mesh', file_size=12)
        groups = {'asset': [part]}
        vertex = b'\x01\x02\x03\x04'
        index = b'\x05\x06'
        body = b'aa' + vertex + b'bb' + index + b'cc'
        mesh = SimpleNamespace(m_Name='example', m_VertexData=SimpleNamespace(m_DataSize=vertex),
                               m_IndexBuffer=index, m_StreamData=None)
        obj = SimpleNamespace(type=SimpleNamespace(name='Mesh'), path_id=4, byte_start=0,
                              read=lambda: mesh, get_raw_data=lambda: body)
        streamed = SimpleNamespace(m_Name='streamed', m_VertexData=SimpleNamespace(m_DataSize=b''),
                                   m_IndexBuffer=b'', m_StreamData=SimpleNamespace(path='asset.resS', size=5))
        obj2 = SimpleNamespace(type=SimpleNamespace(name='Mesh'), path_id=5, byte_start=0,
                               read=lambda: streamed, get_raw_data=lambda: body)
        masked = b'aa' + b'\x00\x02\x00\x04' + b'bb' + b'\x00\x06' + b'cc'
        with patch.object(audit.texture_audit.objects, 'asset_groups', return_value=groups), \
             patch.object(audit.UnityPy, 'load', return_value=SimpleNamespace(objects=[obj, obj2])):
            result = audit.audit(Archive(body), Archive(masked))
        self.assertEqual([r['retained_nonzero_bytes'] for r in result['buffers']], [2, 1])
        self.assertEqual(result['retained_nonzero_bytes'], 3)
        self.assertEqual(result['skipped'][0]['reason'], 'streamed vertex data')

    def test_rejects_mismatched_asset_layout(self):
        first = {'asset': [SimpleNamespace(filename='mesh', file_size=12)]}
        second = {'asset': [SimpleNamespace(filename='mesh', file_size=13)]}
        with patch.object(audit.texture_audit.objects, 'asset_groups', side_effect=[first, second]):
            with self.assertRaisesRegex(ValueError, 'layout differs'):
                audit.audit(Archive(b''), Archive(b''))

    def test_compressed_mesh_payload_is_measured(self):
        part = SimpleNamespace(filename='mesh', file_size=8)
        groups = {'asset': [part]}
        body = b'aa\x03\x04\x05\x06bb'
        compressed = SimpleNamespace(m_Vertices=SimpleNamespace(m_Data=b'\x03\x04\x05\x06'))
        mesh = SimpleNamespace(m_Name='compressed', m_VertexData=SimpleNamespace(m_DataSize=b''),
                               m_IndexBuffer=b'', m_StreamData=None, m_CompressedMesh=compressed)
        obj = SimpleNamespace(type=SimpleNamespace(name='Mesh'), path_id=1, byte_start=0,
                              read=lambda: mesh, get_raw_data=lambda: body)
        with patch.object(audit.texture_audit.objects, 'asset_groups', return_value=groups), \
             patch.object(audit.UnityPy, 'load', return_value=SimpleNamespace(objects=[obj])):
            result = audit.audit(Archive(body), Archive(b'aa\x00\x04\x00\x06bb'))
        self.assertEqual(result['buffers'][0]['kind'], 'compressed/m_Vertices')
        self.assertEqual(result['retained_nonzero_bytes'], 2)


if __name__ == '__main__':
    unittest.main()

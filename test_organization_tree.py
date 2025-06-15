import requests
import pytest

BASE_URL = "http://localhost:5000"
HEADERS = {"Content-Type": "application/xml"}


@pytest.fixture(scope="function", autouse=True)
def clear_test_data():
    yield
    response = requests.get(f"{BASE_URL}/api/OrganizationTree")
    if response.status_code == 200:
        for tree in response.json():
            if "TestTree" in tree["treeName"]:
                requests.delete(f"{BASE_URL}/api/OrganizationTree/{tree['id']}", headers=HEADERS)


def test_create_and_get_tree():
    xml = """
    <Tree>
      <TreeName>TestTree</TreeName>
      <person id="1"><name>Franciszek</name></person>
    </Tree>
    """
    r = requests.post(f"{BASE_URL}/api/OrganizationTree", data=xml, headers=HEADERS)
    assert r.status_code == 201
    tree_id = r.json()["id"]

    r2 = requests.get(f"{BASE_URL}/api/OrganizationTree/{tree_id}")
    assert r2.status_code == 200
    assert "Franciszek" in r2.text


def test_update_tree():
    xml = """
    <Tree>
      <TreeName>TestTree</TreeName>
      <person id="1"><name>Jan</name></person>
    </Tree>
    """
    r = requests.post(f"{BASE_URL}/api/OrganizationTree", data=xml, headers=HEADERS)
    tree_id = r.json()["id"]

    update = """
    <Tree>
      <person id="1"><name>Janusz</name></person>
    </Tree>
    """
    r2 = requests.put(f"{BASE_URL}/api/OrganizationTree/{tree_id}", data=update, headers=HEADERS)
    assert r2.status_code == 204

    r3 = requests.get(f"{BASE_URL}/api/OrganizationTree/{tree_id}")
    assert "Janusz" in r3.text


def test_add_node():
    xml = """
    <Tree>
      <TreeName>TestTree</TreeName>
      <person id="1"><name>Franciszek</name><children /></person>
    </Tree>
    """
    r = requests.post(f"{BASE_URL}/api/OrganizationTree", data=xml, headers=HEADERS)
    tree_id = r.json()["id"]

    patch = """
    <NewNode parent="person[@id='1']/children">
      <person id="2"><name>Anna</name></person>
    </NewNode>
    """
    r2 = requests.patch(f"{BASE_URL}/api/OrganizationTree/{tree_id}/addnode", data=patch, headers=HEADERS)
    assert r2.status_code == 200
    assert "Anna" in r2.text


def test_remove_node():
    xml = """
    <Tree>
      <TreeName>TestTree</TreeName>
      <person id="1">
        <name>Franciszek</name>
        <children>
          <person id="2"><name>Anna</name></person>
        </children>
      </person>
    </Tree>
    """
    r = requests.post(f"{BASE_URL}/api/OrganizationTree", data=xml, headers=HEADERS)
    tree_id = r.json()["id"]

    remove = """
    <RemoveNode path="person[@id='1']/children/person[@id='2']"/>
    """
    r2 = requests.delete(f"{BASE_URL}/api/OrganizationTree/{tree_id}/removenode", data=remove, headers=HEADERS)
    assert r2.status_code == 204

    r3 = requests.get(f"{BASE_URL}/api/OrganizationTree/{tree_id}")
    assert "Anna" not in r3.text


def test_generate_report_full_tree():
    xml = """
    <Tree>
      <TreeName>TestTree</TreeName>
      <person id="1"><name>Franciszek</name></person>
    </Tree>
    """
    r = requests.post(f"{BASE_URL}/api/OrganizationTree", data=xml, headers=HEADERS)
    tree_id = r.json()["id"]

    report = requests.get(f"{BASE_URL}/api/OrganizationTree/{tree_id}/report")
    assert report.status_code == 200
    assert "Franciszek" in report.text


def test_generate_report_subtree():
    xml = """
    <Tree>
      <TreeName>TestTree</TreeName>
      <person id="1">
        <name>Franciszek</name>
        <children>
          <person id="2"><name>Anna</name></person>
        </children>
      </person>
    </Tree>
    """
    r = requests.post(f"{BASE_URL}/api/OrganizationTree", data=xml, headers=HEADERS)
    tree_id = r.json()["id"]

    path = "person[@id='1']/children/person[@id='2']"
    r2 = requests.get(f"{BASE_URL}/api/OrganizationTree/{tree_id}/report?path={path}")
    assert r2.status_code == 200
    assert "Anna" in r2.text
    assert "Franciszek" not in r2.text


def test_invalid_xml_create():
    r = requests.post(f"{BASE_URL}/api/OrganizationTree", data="<BadXml>", headers=HEADERS)
    assert r.status_code == 400


def test_invalid_xml_update():
    xml = """
    <Tree>
      <TreeName>TestTree</TreeName>
      <person id="1"><name>Test</name></person>
    </Tree>
    """
    r = requests.post(f"{BASE_URL}/api/OrganizationTree", data=xml, headers=HEADERS)
    tree_id = r.json()["id"]

    r2 = requests.put(f"{BASE_URL}/api/OrganizationTree/{tree_id}", data="<invalid>", headers=HEADERS)
    assert r2.status_code == 400


def test_delete_tree():
    xml = """
    <Tree>
      <TreeName>TestTree</TreeName>
      <person id="1"><name>DeleteMe</name></person>
    </Tree>
    """
    r = requests.post(f"{BASE_URL}/api/OrganizationTree", data=xml, headers=HEADERS)
    tree_id = r.json()["id"]

    r2 = requests.delete(f"{BASE_URL}/api/OrganizationTree/{tree_id}")
    assert r2.status_code == 204

    r3 = requests.get(f"{BASE_URL}/api/OrganizationTree/{tree_id}")
    assert r3.status_code == 404

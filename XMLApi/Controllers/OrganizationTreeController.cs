using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Xml.XPath;
using System.Xml;
using System.Collections.Generic;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class OrganizationTreeController : ControllerBase
{
    private readonly OrganizationTreeContext _context;

    public OrganizationTreeController(OrganizationTreeContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationTree>>> GetAllTrees()
    {
        try
        {
            var trees = await _context.OrganizationTrees.ToListAsync();
            if (trees == null || !trees.Any())
            {
                return NotFound("No trees found.");
            }
            return Ok(trees);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving trees: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationTree>> GetOrganizationTree(int id)
    {
        var tree = await _context.OrganizationTrees.FindAsync(id);
        if (tree == null)
        {
            return NotFound();
        }
        return Ok(tree);
    }

    [HttpPost]
    [Consumes("application/xml")]
    public async Task<ActionResult<OrganizationTree>> CreateOrganizationTree()
    {
        if (Request.Body == null || !Request.Body.CanRead)
        {
            return BadRequest("XML data is required.");
        }

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string xmlContent = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(xmlContent))
            {
                return BadRequest("XML data is required.");
            }

            XElement xmlData = XElement.Parse(xmlContent);
            var treeName = xmlData.Element("TreeName")?.Value ?? "Unnamed Tree";
            var treeData = new XElement("Root", xmlData.Elements().Where(e => e.Name != "TreeName")).ToString(); 

            var tree = new OrganizationTree
            {
                TreeName = treeName,
                TreeData = treeData
            };

            _context.OrganizationTrees.Add(tree);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrganizationTree), new { id = tree.Id }, tree);
        }
        catch (XmlException ex)
        {
            return BadRequest($"Invalid XML data: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    [Consumes("application/xml")]
    public async Task<IActionResult> UpdateOrganizationTree(int id)
    {
        var tree = await _context.OrganizationTrees.FindAsync(id);
        if (tree == null)
        {
            return NotFound();
        }

        if (Request.Body == null || !Request.Body.CanRead)
        {
            return BadRequest("XML data is required.");
        }

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string xmlContent = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(xmlContent))
            {
                return BadRequest("XML data is required.");
            }

            XElement xmlData = XElement.Parse(xmlContent);
            tree.TreeData = new XElement("Root", xmlData.Elements()).ToString(); 
            _context.Entry(tree).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (XmlException ex)
        {
            return BadRequest($"Invalid XML data: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrganizationTree(int id)
    {
        var tree = await _context.OrganizationTrees.FindAsync(id);
        if (tree == null)
        {
            return NotFound();
        }

        _context.OrganizationTrees.Remove(tree);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/addnode")]
    [Consumes("application/xml")]
    public async Task<ActionResult<string>> AddNodeToTree(int id)
    {
        var tree = await _context.OrganizationTrees.FindAsync(id);
        if (tree == null)
            return NotFound();

        if (Request.Body == null || !Request.Body.CanRead)
            return BadRequest("Node XML data is required.");

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string xmlContent = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(xmlContent))
                return BadRequest("Node XML data is required.");

            XElement requestData = XElement.Parse(xmlContent);

            var parentPath = requestData.Attribute("parent")?.Value;
            XElement newNode = requestData.Elements().FirstOrDefault();
            if (newNode == null)
                return BadRequest("No valid child node found inside the <NewNode> wrapper.");

            XElement treeData = XElement.Parse(tree.TreeData ?? "<Root/>");
            XElement? parentNode = string.IsNullOrEmpty(parentPath) ? treeData : treeData.XPathSelectElement(parentPath);

            if (parentNode == null)
                return BadRequest($"Parent node at path '{parentPath}' not found.");

            parentNode.Add(newNode);
            tree.TreeData = treeData.ToString();
            _context.Entry(tree).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(tree.TreeData);
        }
        catch (XmlException ex)
        {
            return BadRequest($"Invalid XML in node value: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error adding node: {ex.Message}");
        }
    }

    [HttpDelete("{id}/removenode")]
    [Consumes("application/xml")]
    public async Task<IActionResult> RemoveNodeFromTree(int id)
    {
        var tree = await _context.OrganizationTrees.FindAsync(id);
        if (tree == null)
        {
            return NotFound();
        }

        if (Request.Body == null || !Request.Body.CanRead)
        {
            return BadRequest("Node path data is required.");
        }

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string xmlContent = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(xmlContent))
            {
                return BadRequest("Node path data is required.");
            }

            XElement removeData = XElement.Parse(xmlContent);
            var nodePath = removeData.Attribute("path")?.Value;
            if (string.IsNullOrEmpty(nodePath))
            {
                return BadRequest("Path attribute is required in the XML.");
            }

            XElement treeData = XElement.Parse(tree.TreeData ?? "<Root/>");
            var nodeToRemove = treeData.XPathSelectElement(nodePath);

            if (nodeToRemove == null)
            {
                return BadRequest($"Node at path '{nodePath}' not found.");
            }

            nodeToRemove.Remove();
            tree.TreeData = treeData.ToString();
            _context.Entry(tree).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (XmlException ex)
        {
            return BadRequest($"Invalid XML data: {ex.Message}");
        }
    }
    [HttpGet("{id}/report")]
public async Task<IActionResult> GenerateSubtreeReport(int id, [FromQuery] string? path)
{
    var tree = await _context.OrganizationTrees.FindAsync(id);
    if (tree == null)
        return NotFound();

    try
    {
        XElement treeData = XElement.Parse(tree.TreeData ?? "<Root/>");

        XElement? targetNode = string.IsNullOrWhiteSpace(path)
            ? treeData
            : treeData.XPathSelectElement(path);

        if (targetNode == null)
            return BadRequest($"Node at path '{path}' not found.");

        return Ok(targetNode.ToString());
    }
    catch (XmlException ex)
    {
        return BadRequest($"Invalid XML: {ex.Message}");
    }
}

    
}
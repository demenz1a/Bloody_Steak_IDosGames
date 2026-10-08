using UnityEngine;

public class OldPlayerPickUp : MonoBehaviour
{
    public Transform holdPoint;
    public OldProduct heldProduct;

    private OldProduct nearbyProduct;
    private OldMeatTable nearbyMeatTable;
    private OldCookStation nearbyCookStation;
    private OldTrashBin nearbyTrash;
    private OldClient nearbyClient;
    private OldTelkaClientInfiniteOrder nearbyTelka;

    public SpriteRenderer ClueSpriteRenderer;

    [Header("Debug")]
    public bool debugClue = true;

    private string lastClueCaller;
    private GameObject lastClueObject;
    private bool lastClueEnabled;
    public Animator animator;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (nearbyCookStation != null)
        {
            nearbyCookStation.Interact(this);
            return;
        }

        if (nearbyMeatTable != null)
        {
            nearbyMeatTable.Interact(this);
            return;
        }

        if (nearbyProduct != null)
        {
            TakeProduct(nearbyProduct);
            return;
        }

        if (nearbyTrash != null)
        {
            nearbyTrash.Interact(this);
            return;
        }

        if (nearbyClient != null)
        {
            nearbyClient.Interact(this);
            return;
        }

        if (nearbyTelka != null)
        {
            nearbyTelka.Interact(this);
            return;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out OldProduct product))
            nearbyProduct = product;
        //ClueSpriteRenderer.enabled = true;

        if (other.TryGetComponent(out OldMeatTable table))
            nearbyMeatTable = table;
        //ClueSpriteRenderer.enabled = true;

        if (other.TryGetComponent(out OldCookStation station))
            nearbyCookStation = station;
        //ClueSpriteRenderer.enabled = true;

        if (other.TryGetComponent(out OldTrashBin trash))
            nearbyTrash = trash;
        //ClueSpriteRenderer.enabled = true;

        if (other.TryGetComponent(out OldClient client))
            nearbyClient = client;
        //ClueSpriteRenderer.enabled = true;

        if (other.TryGetComponent(out OldTelkaClientInfiniteOrder telkaclient))
        {
            nearbyTelka = telkaclient;
            animator.SetTrigger("Horny");
        }

        //ClueSpriteRenderer.enabled = true;

        UpdateClue("ENTER", other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<OldProduct>() == nearbyProduct)
            //ClueSpriteRenderer.enabled = false;
            nearbyProduct = null;

        if (other.GetComponent<OldMeatTable>() == nearbyMeatTable)
            //ClueSpriteRenderer.enabled = false;
            nearbyMeatTable = null;

        if (other.GetComponent<OldCookStation>() == nearbyCookStation)
            //ClueSpriteRenderer.enabled = false;
            nearbyCookStation = null;

        if (other.GetComponent<OldTrashBin>() == nearbyTrash)
            //ClueSpriteRenderer.enabled = false;
            nearbyTrash = null;

        if (other.GetComponent<OldClient>() == nearbyClient)
            //ClueSpriteRenderer.enabled = false;
            nearbyClient = null;

        if (other.GetComponent<OldTelkaClientInfiniteOrder>() == nearbyTelka)
            //ClueSpriteRenderer.enabled = false;
            nearbyTelka = null;

        UpdateClue("EXIT", other);
    }

    public void TakeProduct(OldProduct product)
    {
        if (heldProduct != null) return;

        heldProduct = product;
        product.transform.SetParent(holdPoint);
        product.transform.localPosition = Vector3.zero;
    }

    public void DropProduct()
    {
        if (heldProduct == null) return;

        heldProduct.transform.SetParent(null);
        heldProduct = null;
    }

    void UpdateClue(string caller, Collider2D other)
    {
        if (ClueSpriteRenderer == null) return;

        bool canInteract =
            nearbyCookStation != null ||
            nearbyMeatTable != null ||
            nearbyProduct != null ||
            nearbyTrash != null ||
            nearbyClient != null ||
            nearbyTelka != null;

        ClueSpriteRenderer.enabled = canInteract;

        if (debugClue)
        {
            lastClueCaller = caller;
            lastClueObject = other != null ? other.gameObject : null;

            if (lastClueEnabled != canInteract)
            {
                lastClueEnabled = canInteract;

                string who = lastClueObject != null
                    ? $"{lastClueObject.name} ({lastClueObject.GetType().Name})"
                    : "null";

                Debug.Log(
                    $"[Clue] enabled={canInteract} | calledFrom={caller} | other={who}\n" +
                    $"Nearby: product={(nearbyProduct ? nearbyProduct.name : "null")}, " +
                    $"meatTable={(nearbyMeatTable ? nearbyMeatTable.name : "null")}, " +
                    $"cookStation={(nearbyCookStation ? nearbyCookStation.name : "null")}, " +
                    $"trash={(nearbyTrash ? nearbyTrash.name : "null")}, " +
                    $"client={(nearbyClient ? nearbyClient.name : "null")}, " +
                    $"telka={(nearbyTelka ? nearbyTelka.name : "null")}"
                );
            }
        }
    }


}